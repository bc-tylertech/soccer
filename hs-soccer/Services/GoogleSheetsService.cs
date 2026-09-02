using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HsSoccer.Models;

namespace HsSoccer.Services
{
	public class GoogleSheetsService
	{
		private readonly string _credentialsPath;
		private SheetsService _service;

		public GoogleSheetsService( string credentialsPath = "credentials.json" )
		{
			_credentialsPath = credentialsPath;
		}

		public async Task InitializeAsync()
		{
			var credFile = _credentialsPath;
			if ( !File.Exists( credFile ) )
			{
				credFile = Path.Combine( Directory.GetCurrentDirectory(), "credentials.json" );
			}

			if ( !File.Exists( credFile ) )
			{
				throw new FileNotFoundException( "Google Service Account credentials.json not found!", _credentialsPath );
			}

			var scopes = new[]
			{
				SheetsService.Scope.Spreadsheets,
				SheetsService.Scope.DriveFile,
				"https://www.googleapis.com/auth/drive"
			};

			using ( var stream = new FileStream( credFile, FileMode.Open, FileAccess.Read ) )
			{
				var googleCred = GoogleCredential.FromStream( stream ).CreateScoped( scopes );
				_service = new SheetsService( new BaseClientService.Initializer()
				{
					HttpClientInitializer = googleCred,
					ApplicationName = "High School Soccer Volunteer System"
				} );
			}
		}

		public async Task DeleteExtraColumnsAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			try
			{
				var spreadsheet = await _service.Spreadsheets.Get( spreadsheetId ).ExecuteAsync();
				var sheet3 = spreadsheet.Sheets.FirstOrDefault( s => s.Properties.Title.Equals( "Form Responses 3", StringComparison.OrdinalIgnoreCase ) );

				if ( sheet3 != null )
				{
					var hideColsReq = new Request
					{
						UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
						{
							Range = new DimensionRange
							{
								SheetId = sheet3.Properties.SheetId,
								Dimension = "COLUMNS",
								StartIndex = 6, // Column G onwards
								EndIndex = sheet3.Properties.GridProperties.ColumnCount
							},
							Properties = new DimensionProperties
							{
								HiddenByUser = true
							},
							Fields = "hiddenByUser"
						}
					};

					if ( sheet3.Properties.GridProperties.ColumnCount > 6 )
					{
						var batchReq = new BatchUpdateSpreadsheetRequest { Requests = new List<Request> { hideColsReq } };
						await _service.Spreadsheets.BatchUpdate( batchReq, spreadsheetId ).ExecuteAsync();
						Console.WriteLine( "SUCCESS: Hid all extra columns past Column F on Form Responses 3 tab!" );
					}
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Note on deleting extra columns: " + ex.Message );
			}
		}

		public async Task CleanFormResponseTabsAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			await DeleteExtraColumnsAsync( spreadsheetId );

			// Clean Form Responses 3 (Dues Log) to standard 6 columns
			var range3 = "'Form Responses 3'!A1:F2";
			var valueRange3 = new ValueRange
			{
				Values = new List<IList<object>>
				{
					new List<object>
					{
						"Timestamp", "Date Collected", "Player Name", "Amount Collected (USD)", "Payment Method", "Logged By (Co-Manager)"
					},
					new List<object>
					{
						DateTime.Now.ToString( "g" ), "8/30/2026", "Christensen, Lukas (Grade 9)", "75", "PayPal", "Brian Christensen (Co-Manager)"
					}
				}
			};

			var clearReq3 = _service.Spreadsheets.Values.Clear( new ClearValuesRequest(), spreadsheetId, "'Form Responses 3'!A1:Z100" );
			await clearReq3.ExecuteAsync();

			var updateReq3 = _service.Spreadsheets.Values.Update( valueRange3, spreadsheetId, range3 );
			updateReq3.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateReq3.ExecuteAsync();

			Console.WriteLine( "SUCCESS: Cleaned Form Responses 3 tab to standard 6-column layout." );
		}

		public async Task CleanAndFixDinnerResponsesTabAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var range = "'Dinner Responses'!A1:F2";
			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>
				{
					new List<object>
					{
						"Timestamp", "Parent/Guardian Name", "Player Name", "Select Dinner Date", "Which role can you cover for this dinner?", "Specific Food Item / Notes"
					},
					new List<object>
					{
						"8/31/2026 16:27:28", "Rory", "Christensen, Lukas (Grade 9)", "2026-09-28", "Drinks (Water / Juice / Gatorade)", "Water / Gatorade"
					}
				}
			};

			var clearReq = _service.Spreadsheets.Values.Clear( new ClearValuesRequest(), spreadsheetId, "'Dinner Responses'!A1:Z100" );
			await clearReq.ExecuteAsync();

			var updateReq = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateReq.ExecuteAsync();

			Console.WriteLine( "SUCCESS: Cleaned and normalized 'Dinner Responses' tab to standard 6 columns and migrated Rory's signup!" );
		}

		public async Task InspectAllTabsAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var spreadsheet = await _service.Spreadsheets.Get( spreadsheetId ).ExecuteAsync();
			Console.WriteLine( "=== SPREADSHEET TABS LIST (" + spreadsheet.Sheets.Count + " tabs) ===" );
			foreach ( var s in spreadsheet.Sheets )
			{
				Console.WriteLine( " - Tab Name: '" + s.Properties.Title + "'" );
			}
			Console.WriteLine( "==========================================" );

			foreach ( var sheet in spreadsheet.Sheets )
			{
				var title = sheet.Properties.Title;
				Console.WriteLine( "\nTab: '" + title + "'" );

				var range = "'" + title + "'!A1:Z20";
				var res = await _service.Spreadsheets.Values.Get( spreadsheetId, range ).ExecuteAsync();

				if ( res.Values != null && res.Values.Count > 0 )
				{
					for ( int i = 0; i < Math.Min( 10, res.Values.Count ); i++ )
					{
						var rowStr = string.Join( " | ", res.Values[i] );
						Console.WriteLine( "  Row " + ( i + 1 ) + ": " + rowStr );
					}
				}
				else
				{
					Console.WriteLine( "  (Empty tab)" );
				}
			}
		}

		public async Task<List<Player>> GetLiveUnpaidPlayersAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var response = await _service.Spreadsheets.Values.Get( spreadsheetId, "'Roster'!A2:J100" ).ExecuteAsync();
			var unpaid = new List<Player>();

			if ( response.Values != null )
			{
				foreach ( var row in response.Values )
				{
					if ( row.Count >= 9 )
					{
						var idStr = row[0]?.ToString() ?? "0";
						int.TryParse( idStr, out int id );
						var lastName = row[1]?.ToString() ?? "";
						var firstName = row[2]?.ToString() ?? "";
						var grade = row[3]?.ToString() ?? "";

						var balanceStr = row[8]?.ToString() ?? "75";
						decimal.TryParse( balanceStr.Replace( "$", "" ).Trim(), out decimal balance );

						var status = row.Count >= 10 ? row[9]?.ToString() ?? "" : "";

						if ( balance > 0 && !status.Equals( "Paid", StringComparison.OrdinalIgnoreCase ) )
						{
							unpaid.Add( new Player
							{
								Id = id,
								LastName = lastName,
								FirstName = firstName,
								Grade = grade,
								DuesRequired = 75.00m,
								DuesPaid = 75.00m - balance
							} );
						}
					}
				}
			}

			return unpaid;
		}

		public async Task<List<string>> GetLiveDinnerDatesAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var response = await _service.Spreadsheets.Values.Get( spreadsheetId, "'Team Dinners'!A2:H20" ).ExecuteAsync();
			var dates = new List<string>();

			if ( response.Values != null )
			{
				foreach ( var row in response.Values )
				{
					if ( row.Count >= 1 )
					{
						var d = row[0]?.ToString()?.Trim() ?? "";
						var countStr = row.Count >= 8 ? row[7]?.ToString()?.Trim() ?? "0" : "0";
						int.TryParse( countStr, out int count );

						bool isPast = false;
						if ( DateTime.TryParse( d, out var dt ) )
						{
							if ( dt < DateTime.Today.AddDays( -1 ) )
							{
								isPast = true;
							}
						}

						// Keep date if active, NOT past, AND has fewer than 4 volunteers!
						if ( !string.IsNullOrWhiteSpace( d ) && !d.StartsWith( "#" ) && !isPast && count < 4 )
						{
							dates.Add( d );
						}
					}
				}
			}

			return dates;
		}

		public async Task SetupSpreadsheetStructureAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var spreadsheet = await _service.Spreadsheets.Get( spreadsheetId ).ExecuteAsync();
			var existingSheets = spreadsheet.Sheets.Select( s => s.Properties.Title ).ToList();

			var requiredTabs = new List<string>
			{
				"📌 Start Here (Dashboard)",
				"Roster",
				"Budget Ledger",
				"Dues Log (Co-Managers)",
				"Reimbursements",
				"Team Dinners",
				"Dinner Signup Status (Public)",
				"Master Schedule",
				"Team Info"
			};

			var requests = new List<Request>();

			foreach ( var tab in requiredTabs )
			{
				if ( !existingSheets.Contains( tab ) )
				{
					var addSheetRequest = new Request
					{
						AddSheet = new AddSheetRequest
						{
							Properties = new SheetProperties
							{
								Title = tab,
								Index = tab == "📌 Start Here (Dashboard)" ? 0 : (int?)null
							}
						}
					};
					requests.Add( addSheetRequest );
				}
			}

			if ( requests.Count > 0 )
			{
				var batchUpdate = new BatchUpdateSpreadsheetRequest
				{
					Requests = requests
				};
				await _service.Spreadsheets.BatchUpdate( batchUpdate, spreadsheetId ).ExecuteAsync();
			}
		}

		public async Task SeedDashboardTabAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var clearRequest = _service.Spreadsheets.Values.Clear( new ClearValuesRequest(), spreadsheetId, "'📌 Start Here (Dashboard)'!A1:Z100" );
			await clearRequest.ExecuteAsync();

			var range = "'📌 Start Here (Dashboard)'!A1:D30";

			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>
				{
					new List<object> { "-----------------------------------------------------------------------------------------------------------------------" },
					new List<object> { "  OREGON HIGH SCHOOL JV2 BOYS SOCCER (2026) - MASTER MANAGEMENT DASHBOARD  " },
					new List<object> { "-----------------------------------------------------------------------------------------------------------------------" },
					new List<object> { "" },
					new List<object> { "Co-Managers:", "Brian Christensen & Megan Rueth" },
					new List<object> { "Team:", "Oregon High School JV2 Boys Soccer (Panthers)" },
					new List<object> { "Drive Folder:", "https://drive.google.com/drive/folders/1q7vy8NL92cbpIQOI-7BmI0DuLISrX0Yp" },
					new List<object> { "" },
					new List<object> { "--- 🔗 QUICK RESOURCE & FORM LINKS ---" },
					new List<object> { "Resource Name", "Link / Access URL", "Audience", "Description" },
					new List<object> { "🥗 Team Dinner Sign-Up Form", "https://docs.google.com/forms/d/1Ol68WmioL42GO_n47N6Cq3g30o_meeqYk9Hjn2SqPD8/viewform", "All Parents", "Form for parents to sign up for dinner dates (Min 3 / Max 5 per date)" },
					new List<object> { "💳 Expense Reimbursement Form", "https://docs.google.com/forms/d/1a38G6PpgwZrqMzIUNCVxXQwvLsdP-va-jLdnUzxE91o/viewform", "Co-Managers & Volunteers", "Form to submit Gatorade, gift card & sub receipts" },
					new List<object> { "💵 $75 Dues Collection Form", "https://docs.google.com/forms/d/1RnY-KJ-r29IKLJN_rtXYahsWdu6TNepmpINRRY4No18/viewform", "Co-Managers Only", "Form for Brian & Megan to record $75 fee receipts" },
					new List<object> { "🌐 Public Parent Web Portal", "https://bc-tylertech.github.io/soccer/", "All Parents", "Live dark-mode website showing dinner status & game schedule" },
					new List<object> { "📅 Official GoBound Match Schedule", "https://www.gobound.com/wi/wiaa/bsc/2026-27/oregon/jv2/schedule", "Public", "Official High School Athletic Association Match Schedule" },
					new List<object> { "" },
					new List<object> { "--- 📊 SPREADSHEET TAB NAVIGATION & GUIDE ---" },
					new List<object> { "Tab Name", "Purpose & Contents", "Access Level", "Key Formulas / Automation" },
					new List<object> { "Roster", "23 Players, Parent Emails, Dues Balance", "Co-Managers Only", "Dynamic Balance = Required ($75) - Paid" },
					new List<object> { "Budget Ledger", "$1,701 Budget Breakdown & Expenses", "Co-Managers Only", "Live Category Sums & Remaining Balances" },
					new List<object> { "Dues Log (Co-Managers)", "Private $75 Fee Collection Receipts Log", "Co-Managers Only", "Records Date, Player, Payment Method (Venmo/PayPal/Cash/Check)" },
					new List<object> { "Reimbursements", "Expense Reimbursement Ledger & Receipts", "Co-Managers Only", "Tracks Paid vs Pending Claims" },
					new List<object> { "Team Dinners", "7 Dinner Dates & Volunteer Assignments", "Co-Managers Only", "Capacity Limits: Min 3 / Max 5 Auto-Status" },
					new List<object> { "Dinner Signup Status (Public)", "Read-Only Dinner Summary for Parents", "Public / Published", "Published to Web CSV for Public Dashboard" },
					new List<object> { "Master Schedule", "31 Events (19 Games, Dinners, Service)", "Co-Managers Only", "GoBound Schedule & Volunteer Service Dates" }
				}
			};

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();

			// Explicitly clear and update cell B14 to purge old hyperlink formatting
			try
			{
				var clearB14 = _service.Spreadsheets.Values.Clear( new ClearValuesRequest(), spreadsheetId, "'📌 Start Here (Dashboard)'!B14" );
				await clearB14.ExecuteAsync();

				var valB14 = new ValueRange { Values = new List<IList<object>> { new List<object> { "https://bc-tylertech.github.io/soccer/" } } };
				var updateB14 = _service.Spreadsheets.Values.Update( valB14, spreadsheetId, "'📌 Start Here (Dashboard)'!B14" );
				updateB14.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
				await updateB14.ExecuteAsync();
			}
			catch { }
		}

		public async Task SeedRosterAsync( string spreadsheetId, List<Player> players )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var range = "Roster!A1:J" + ( players.Count + 10 );

			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>()
			};

			var headers = new List<object>
			{
				"Player ID", "Last Name", "First Name", "Grade", "Player Email",
				"Parent Email(s)", "Dues Required ($)", "Dues Paid ($)", "Balance ($)", "Status"
			};
			valueRange.Values.Add( headers );

			foreach ( var player in players )
			{
				var rIndex = valueRange.Values.Count + 1;
				var parentEmails = string.Join( ", ", player.ParentEmails );
				var row = new List<object>
				{
					player.Id,
					player.LastName,
					player.FirstName,
					player.Grade,
					player.PlayerEmail,
					parentEmails,
					player.DuesRequired,
					"=IFERROR(SUMIF('Dues Responses'!C:C, \"*\" & B" + rIndex + " & \"*\", 'Dues Responses'!D:D), 0) + IFERROR(SUMIF('Dues Responses'!C:C, \"*\" & B" + rIndex + " & \"*\", 'Dues Responses'!I:I), 0) + IFERROR(SUMIF('Dues Responses'!H:H, \"*\" & B" + rIndex + " & \"*\", 'Dues Responses'!I:I), 0)",
					"=G" + rIndex + "-H" + rIndex,
					"=IF(I" + rIndex + "<=0,\"Paid\",IF(H" + rIndex + ">0,\"Partial\",\"Unpaid\"))"
				};
				valueRange.Values.Add( row );
			}

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
		}

		public async Task SeedBudgetAsync( string spreadsheetId, List<BudgetItem> budgetItems )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var range = "Budget Ledger!A1:E" + ( budgetItems.Count + 10 );

			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>()
			};

			var headers = new List<object>
			{
				"Category", "Description", "Allocated Amount ($)", "Spent Amount ($)", "Remaining Balance ($)"
			};
			valueRange.Values.Add( headers );

			foreach ( var item in budgetItems )
			{
				var row = new List<object>
				{
					item.Category,
					item.Description,
					item.AllocatedAmount,
					item.SpentAmount,
					"=C" + ( valueRange.Values.Count + 1 ) + "-D" + ( valueRange.Values.Count + 1 )
				};
				valueRange.Values.Add( row );
			}

			var totalRow = new List<object>
			{
				"TOTALS",
				"All Season Expenses",
				"=SUM(C2:C" + valueRange.Values.Count + ")",
				"=SUM(D2:D" + valueRange.Values.Count + ")",
				"=SUM(E2:E" + valueRange.Values.Count + ")"
			};
			valueRange.Values.Add( totalRow );

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
		}

		public async Task SeedScheduleAsync( string spreadsheetId, List<ScheduleItem> scheduleItems )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var range = "Master Schedule!A1:F" + ( scheduleItems.Count + 10 );

			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>()
			};

			var headers = new List<object>
			{
				"Date", "Time", "Category", "Opponent / Event", "Location / Field", "Volunteer & Admin Notes"
			};
			valueRange.Values.Add( headers );

			foreach ( var item in scheduleItems )
			{
				var row = new List<object>
				{
					item.Date,
					item.Time,
					item.Category,
					item.OpponentOrEvent,
					item.Location,
					item.Notes
				};
				valueRange.Values.Add( row );
			}

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
		}

		public async Task SeedDinnersAsync( string spreadsheetId, List<TeamDinnerSlot> dinnerSlots )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var range = "Team Dinners!A1:I" + ( dinnerSlots.Count + 10 );

			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>()
			};

			var headers = new List<object>
			{
				"Dinner Date", "Host Family", "Location", "Main Course Volunteer", "Drinks Volunteer", "Dessert Volunteer", "Sides Volunteer", "Signed-Up Count", "Status"
			};
			valueRange.Values.Add( headers );

			foreach ( var slot in dinnerSlots )
			{
				var rIndex = valueRange.Values.Count + 1;
				var row = new List<object>
				{
					slot.Date,
					slot.HostFamily,
					slot.Location,
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Main\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Drink\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Dessert\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Side\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IF(D" + rIndex + "=\"Unassigned\", 0, LEN(D" + rIndex + ")-LEN(SUBSTITUTE(D" + rIndex + ", \",\", \"\"))+1) + (E" + rIndex + "<>\"Unassigned\") + (F" + rIndex + "<>\"Unassigned\") + (G" + rIndex + "<>\"Unassigned\")",
					"=IF(H" + rIndex + ">=5, \"FULL (5 Volunteers)\", IF(H" + rIndex + ">=4, \"Confirmed (4/5 Volunteers)\", IF(H" + rIndex + ">=3, \"Confirmed (3/5 Volunteers)\", \"Needs Volunteers (\" & (5-H" + rIndex + ") & \" Needed)\")))"
				};
				valueRange.Values.Add( row );
			}

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
		}

		public async Task SeedReimbursementsAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var range = "Reimbursements!A1:J35";
			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>()
			};

			valueRange.Values.Add( new List<object> { "--- 💵 CO-MANAGER REIMBURSEMENT CASH POOL SUMMARY ---" } );
			valueRange.Values.Add( new List<object> { "Total Dues Collected (Roster):", "=IFERROR(SUM(Roster!H2:H), 0)" } );
			valueRange.Values.Add( new List<object> { "Total Reimbursements Paid Out:", "=IFERROR(SUMIF(I8:I100, \"Paid\", E8:E100), 0)" } );
			valueRange.Values.Add( new List<object> { "Current Available Cash Balance:", "=B2-B3", "", "=IF(B4<0, \"🚨 CRITICAL DEFICIT\", IF(B4<100, \"⚠️ LOW FUNDS\", \"🟢 HEALTHY BALANCE\"))" } );
			valueRange.Values.Add( new List<object> { "" } );
			valueRange.Values.Add( new List<object> { "--- 📋 SUBMITTED EXPENSE REIMBURSEMENT CLAIMS ---" } );

			var headers = new List<object>
			{
				"Timestamp", "Purchaser Name", "Purchase Date", "Expense Category", "Amount Spent ($)", "Description / Store", "Receipt Link", "Fund Availability Check", "Reimbursement Status", "Reimbursed By"
			};
			valueRange.Values.Add( headers );

			for ( int r = 8; r <= 35; r++ )
			{
				var rIndex = r;
				var sourceRow = rIndex - 6; // Form Responses start at Row 2 (when r=8, sourceRow=2)

				var row = new List<object>
				{
					"=IFERROR(INDEX('Expenses Responses'!A:A, " + sourceRow + "), IFERROR(INDEX('Form Responses 2'!A:A, " + sourceRow + "), \"\"))",
					"=IFERROR(INDEX('Expenses Responses'!C:C, " + sourceRow + "), IFERROR(INDEX('Form Responses 2'!C:C, " + sourceRow + "), \"\"))",
					"=IFERROR(INDEX('Expenses Responses'!D:D, " + sourceRow + "), IFERROR(INDEX('Form Responses 2'!D:D, " + sourceRow + "), \"\"))",
					"=IFERROR(INDEX('Expenses Responses'!E:E, " + sourceRow + "), IFERROR(INDEX('Form Responses 2'!E:E, " + sourceRow + "), \"\"))",
					"=IFERROR(INDEX('Expenses Responses'!F:F, " + sourceRow + "), IFERROR(INDEX('Form Responses 2'!F:F, " + sourceRow + "), \"\"))",
					"=IFERROR(INDEX('Expenses Responses'!G:G, " + sourceRow + "), IFERROR(INDEX('Form Responses 2'!G:G, " + sourceRow + "), \"\"))",
					"=IFERROR(INDEX('Expenses Responses'!H:H, " + sourceRow + "), IFERROR(INDEX('Form Responses 2'!H:H, " + sourceRow + "), \"\"))",
					"=IF(LEN(A" + rIndex + ")=0, \"\", IF(I" + rIndex + "=\"Paid\", \"✅ Reimbursed\", IF(E" + rIndex + "<=$B$4, \"🟢 Approved (Funds Available)\", \"🔴 ON HOLD (Insufficient Funds)\")))",
					"Pending",
					""
				};
				valueRange.Values.Add( row );
			}

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
			Console.WriteLine( "SUCCESS: Seeded 'Reimbursements' tab with live dues cash pool formulas and fund availability guardrails!" );
		}

		public async Task SeedDuesLogAsync( string spreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var range = "Dues Log (Co-Managers)!A1:F2";
			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>
				{
					new List<object>
					{
						"Timestamp", "Date Collected", "Player Name", "Amount Collected ($75)", "Payment Method (Venmo/PayPal/Cash/Check)", "Logged By (Parent Manager)"
					},
					new List<object>
					{
						"=IFERROR(FILTER('Form Responses 3'!A2:F, 'Form Responses 3'!A2:A<>\"), \"No form submissions yet\")"
					}
				}
			};

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
		}

		public async Task SeedPublicDinnerStatusAsync( string spreadsheetId, List<TeamDinnerSlot> dinnerSlots )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			var spreadsheet = await _service.Spreadsheets.Get( spreadsheetId ).ExecuteAsync();
			var targetSheet = spreadsheet.Sheets.FirstOrDefault( s => s.Properties.Title.Contains( "Dinner Signup Status", StringComparison.OrdinalIgnoreCase ) );
			if ( targetSheet == null ) return; // Skip if tab doesn't exist
			string targetTitle = targetSheet.Properties.Title;

			var range = "'" + targetTitle + "'!A1";

			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>()
			};

			var headers = new List<object>
			{
				"Dinner Date", "Host Family", "Location", "Main Course Volunteer", "Drinks Volunteer", "Dessert Volunteer", "Sides Volunteer", "Signed-Up Count", "Status"
			};
			valueRange.Values.Add( headers );

			foreach ( var slot in dinnerSlots )
			{
				var rIndex = valueRange.Values.Count + 1;
				var row = new List<object>
				{
					slot.Date,
					slot.HostFamily,
					slot.Location,
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Main\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Drink\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Dessert\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\" & IF(LEN('Dinner Responses'!G$2:G)>0, \" - \" & 'Dinner Responses'!G$2:G, \"\"), (('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E)))*ISNUMBER(SEARCH(\"Side\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=COUNTIF('Dinner Responses'!E:E, A" + rIndex + ")",
					"=IF(H" + rIndex + ">=4, \"FULL (4 Volunteers)\", IF(H" + rIndex + ">=3, \"Confirmed (3/4 Volunteers)\", \"Needs Volunteers (\" & (4-H" + rIndex + ") & \" Needed)\"))"
				};
				valueRange.Values.Add( row );
			}

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
			Console.WriteLine( "Seeding 'Dinner Signup Status (Public)' tab..." );
		}

		public async Task SeedTeamInfoTabAsync( string spreadsheetId, List<TeamDinnerSlot> dinnerSlots = null )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			if ( dinnerSlots == null )
			{
				var rm = new RosterManager();
				dinnerSlots = rm.GenerateDefaultDinnerSlots();
			}

			// Ensure tab named 'Team Info' exists
			try
			{
				var spreadsheetMeta = await _service.Spreadsheets.Get( spreadsheetId ).ExecuteAsync();
				var hasTeamInfo = spreadsheetMeta.Sheets.Any( s => s.Properties.Title == "Team Info" );
				if ( !hasTeamInfo && spreadsheetMeta.Sheets.Count > 0 )
				{
					var firstSheetId = spreadsheetMeta.Sheets[0].Properties.SheetId;
					var renameReq = new BatchUpdateSpreadsheetRequest
					{
						Requests = new List<Request>
						{
							new Request
							{
								UpdateSheetProperties = new UpdateSheetPropertiesRequest
								{
									Properties = new SheetProperties
									{
										SheetId = firstSheetId,
										Title = "Team Info"
									},
									Fields = "title"
								}
							}
						}
					};
					await _service.Spreadsheets.BatchUpdate( renameReq, spreadsheetId ).ExecuteAsync();
				}
			}
			catch { }

			var range = "'Team Info'!A1:I30";
			var valueRange = new ValueRange
			{
				Values = new List<IList<object>>
				{
					new List<object> { "Setting / Item", "Value" },
					new List<object> { "Team Dues Amount", "$75.00" },
					new List<object> { "Dues Due Date", "Wednesday, September 2nd" },
					new List<object> { "Venmo Handle", "@Megan-Rueth-1" },
					new List<object> { "Venmo Phone Last 4", "5983" },
					new List<object> { "PayPal Handle", "@meganrueth" },
					new List<object> { "PayPal Phone Last 4", "5983" },
					new List<object> { "Cash Check Note", "Personal check or cash is accepted at Tuesday's game. Please message Megan and include player's name in memo!" },
					new List<object> { "Hardship Note", "If this is a hardship, please let Megan or Brian know as scholarship funds are available." },
					new List<object> { "Budget Dinner Coach", "$200.00" },
					new List<object> { "" },
					new List<object> { "--- 🥗 LIVE TEAM DINNER SIGN-UP STATUS (PUBLIC) ---" },
					new List<object> { "Dinner Date", "Host Family", "Location", "Main Course Volunteer", "Drinks Volunteer", "Dessert Volunteer", "Sides Volunteer", "Signed-Up Count", "Status" }
				}
			};

			foreach ( var slot in dinnerSlots )
			{
				var rIndex = valueRange.Values.Count + 1;
				var dateMatchExpr = "(TEXT('Dinner Responses'!E$2:E, \"yyyy-mm-dd\")=A" + rIndex + ") + (TEXT('Dinner Responses'!E$2:E, \"m/d/yyyy\")=A" + rIndex + ") + ('Dinner Responses'!E$2:E=A" + rIndex + ") + ISNUMBER(SEARCH(A" + rIndex + ", 'Dinner Responses'!E$2:E & \"\"))";

				var row = new List<object>
				{
					slot.Date,
					slot.HostFamily,
					slot.Location,
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\", (" + dateMatchExpr + ")*ISNUMBER(SEARCH(\"Main\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\", (" + dateMatchExpr + ")*ISNUMBER(SEARCH(\"Drink\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\", (" + dateMatchExpr + ")*ISNUMBER(SEARCH(\"Dessert\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IFERROR(TEXTJOIN(\", \", TRUE, FILTER('Dinner Responses'!C$2:C & \" (\" & 'Dinner Responses'!D$2:D & \")\", (" + dateMatchExpr + ")*ISNUMBER(SEARCH(\"Side\", 'Dinner Responses'!F$2:F)))), \"Unassigned\")",
					"=IF(D" + rIndex + "=\"Unassigned\", 0, LEN(D" + rIndex + ")-LEN(SUBSTITUTE(D" + rIndex + ", \",\", \"\"))+1) + (E" + rIndex + "<>\"Unassigned\") + (F" + rIndex + "<>\"Unassigned\") + (G" + rIndex + "<>\"Unassigned\")",
					"=IF(H" + rIndex + ">=5, \"FULL (5 Volunteers)\", IF(H" + rIndex + ">=4, \"Confirmed (4/5 Volunteers)\", IF(H" + rIndex + ">=3, \"Confirmed (3/5 Volunteers)\", \"Needs Volunteers (\" & (5-H" + rIndex + ") & \" Needed)\")))"
				};
				valueRange.Values.Add( row );
			}

			var updateRequest = _service.Spreadsheets.Values.Update( valueRange, spreadsheetId, range );
			updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			await updateRequest.ExecuteAsync();
			Console.WriteLine( "SUCCESS: Seeded 'Team Info' tab with Dues info & bare-minimum Public Team Dinner Status table!" );
		}

		public async Task<string> CreatePublicFeedSpreadsheetAsync( string masterSpreadsheetId, List<TeamDinnerSlot> dinnerSlots )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			Console.WriteLine( "Creating brand-new separate Public Spreadsheet File in shared Drive folder..." );

			using var stream = new FileStream( _credentialsPath, FileMode.Open, FileAccess.Read );
			var googleCred = GoogleCredential.FromStream( stream ).CreateScoped( new[] { DriveService.Scope.Drive } );
			var driveSvc = new DriveService( new BaseClientService.Initializer()
			{
				HttpClientInitializer = googleCred,
				ApplicationName = "High School Soccer Volunteer System"
			} );

			var fileMetadata = new Google.Apis.Drive.v3.Data.File
			{
				Name = "Oregon JV2 Soccer - Public Portal Feed",
				MimeType = "application/vnd.google-apps.spreadsheet",
				Parents = new List<string> { "1q7vy8NL92cbpIQOI-7BmI0DuLISrX0Yp" }
			};

			var createReq = driveSvc.Files.Create( fileMetadata );
			createReq.SupportsAllDrives = true;
			createReq.Fields = "id";
			var createdFile = await createReq.ExecuteAsync();
			var publicSheetId = createdFile.Id;
			Console.WriteLine( "SUCCESS: Created Public Sheet File (ID: " + publicSheetId + ")" );

			// Make the new spreadsheet file PUBLIC (Anyone with link can read) via Drive API
			var perm = new Google.Apis.Drive.v3.Data.Permission
			{
				Type = "anyone",
				Role = "reader"
			};
			var permReq = driveSvc.Permissions.Create( perm, publicSheetId );
			permReq.SupportsAllDrives = true;
			await permReq.ExecuteAsync();
			Console.WriteLine( "SUCCESS: Set Drive Permissions on Public Sheet: Anyone with link can view (reader)!" );

			return publicSheetId;
		}

		public async Task SyncPublicSheetFromMasterAsync( string masterSpreadsheetId, string publicSpreadsheetId )
		{
			if ( _service == null )
			{
				await InitializeAsync();
			}

			Console.WriteLine( "Reading computed public summary from Master Sheet 'Team Info' tab..." );
			var getReq = _service.Spreadsheets.Values.Get( masterSpreadsheetId, "'Team Info'!A1:I30" );
			var masterData = await getReq.ExecuteAsync();

			if ( masterData.Values != null && masterData.Values.Count > 0 )
			{
				// Ensure tab named 'Team Info' exists on Public Sheet
				try
				{
					var spreadsheetMeta = await _service.Spreadsheets.Get( publicSpreadsheetId ).ExecuteAsync();
					var hasTeamInfo = spreadsheetMeta.Sheets.Any( s => s.Properties.Title == "Team Info" );
					if ( !hasTeamInfo && spreadsheetMeta.Sheets.Count > 0 )
					{
						var firstSheetId = spreadsheetMeta.Sheets[0].Properties.SheetId;
						var renameReq = new BatchUpdateSpreadsheetRequest
						{
							Requests = new List<Request>
							{
								new Request
								{
									UpdateSheetProperties = new UpdateSheetPropertiesRequest
									{
										Properties = new SheetProperties
										{
											SheetId = firstSheetId,
											Title = "Team Info"
										},
										Fields = "title"
									}
								}
							}
						};
						await _service.Spreadsheets.BatchUpdate( renameReq, publicSpreadsheetId ).ExecuteAsync();
					}
				}
				catch { }

				// Clear public sheet and set Cell A1 to dynamic IMPORTRANGE formula pointing to Master Sheet
				try
				{
					var clearReq = _service.Spreadsheets.Values.Clear( new ClearValuesRequest(), publicSpreadsheetId, "'Team Info'!A1:Z100" );
					await clearReq.ExecuteAsync();

					var formulaVal = new ValueRange
					{
						Values = new List<IList<object>>
						{
							new List<object> { "=IMPORTRANGE(\"" + masterSpreadsheetId + "\", \"'Team Info'!A1:I30\")" }
						}
					};
					var importReq = _service.Spreadsheets.Values.Update( formulaVal, publicSpreadsheetId, "'Team Info'!A1" );
					importReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
					await importReq.ExecuteAsync();
					Console.WriteLine( "SUCCESS: Configured Public Sheet Feed with dynamic real-time IMPORTRANGE formula!" );
				}
				catch ( Exception ex )
				{
					Console.WriteLine( "Warning setting IMPORTRANGE on public sheet: " + ex.Message );
				}
			}
		}
	}
}
