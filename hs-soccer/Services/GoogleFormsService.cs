using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Forms.v1;
using Google.Apis.Forms.v1.Data;
using Google.Apis.Services;
using HsSoccer.Models;

namespace HsSoccer.Services
{
	public class GoogleFormsService
	{
		private readonly string _credentialsPath;
		private FormsService _formsService;
		private DriveService _driveService;

		public GoogleFormsService( string credentialsPath = "credentials.json" )
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
				FormsService.Scope.FormsBody,
				DriveService.Scope.Drive,
				DriveService.Scope.DriveFile
			};

			using ( var stream = new FileStream( credFile, FileMode.Open, FileAccess.Read ) )
			{
				var googleCred = GoogleCredential.FromStream( stream ).CreateScoped( scopes );

				_formsService = new FormsService( new BaseClientService.Initializer()
				{
					HttpClientInitializer = googleCred,
					ApplicationName = "High School Soccer Volunteer System"
				} );

				_driveService = new DriveService( new BaseClientService.Initializer()
				{
					HttpClientInitializer = googleCred,
					ApplicationName = "High School Soccer Volunteer System"
				} );
			}
		}

		public async Task SyncTeamDinnerFormAsync( string formId, List<Player> roster, List<string> activeDinnerDates )
		{
			Console.WriteLine( "Note: Google Form modification is disabled to protect form structure." );
			await Task.CompletedTask;
			return;
		}

		public async Task ConfigureDuesFormAsync( string formId, List<Player> rosterPlayers )
		{
			if ( _formsService == null )
			{
				await InitializeAsync();
			}

			var currentForm = await _formsService.Forms.Get( formId ).ExecuteAsync();
			Console.WriteLine( "Configuring Dues Form: " + currentForm.Info.Title + " (ID: " + formId + ")" );

			var requests = new List<Request>();

			string playerItemId = null;
			int playerItemIndex = 0;

			string amountItemId = null;
			int amountItemIndex = 0;

			string paymentMethodItemId = null;
			int paymentMethodItemIndex = 0;

			string loggedByItemId = null;
			int loggedByItemIndex = 0;

			if ( currentForm.Items != null )
			{
				for ( int i = 0; i < currentForm.Items.Count; i++ )
				{
					var item = currentForm.Items[i];
					var title = item.Title ?? "";
					Console.WriteLine( "  Dues Form Item [" + i + "]: '" + title + "' (ID: " + item.ItemId + ")" );

					if ( title.Contains( "Player", StringComparison.OrdinalIgnoreCase ) )
					{
						playerItemId = item.ItemId;
						playerItemIndex = i;
					}
					else if ( title.Contains( "Amount", StringComparison.OrdinalIgnoreCase ) )
					{
						amountItemId = item.ItemId;
						amountItemIndex = i;
					}
					else if ( title.Contains( "Payment", StringComparison.OrdinalIgnoreCase ) || title.Contains( "Method", StringComparison.OrdinalIgnoreCase ) )
					{
						paymentMethodItemId = item.ItemId;
						paymentMethodItemIndex = i;
					}
					else if ( title.Contains( "Logged", StringComparison.OrdinalIgnoreCase ) || title.Contains( "Manager", StringComparison.OrdinalIgnoreCase ) )
					{
						loggedByItemId = item.ItemId;
						loggedByItemIndex = i;
					}
				}
			}

			var playerOptions = rosterPlayers.Select( p => new Option { Value = p.LastName + ", " + p.FirstName + " (Grade " + p.Grade + ")" } ).ToList();

			var amountOptions = new List<Option>
			{
				new Option { Value = "75.00" },
				new Option { Value = "37.50" },
				new Option { IsOther = true }
			};

			var paymentMethodOptions = new List<Option>
			{
				new Option { Value = "Venmo" },
				new Option { Value = "PayPal" },
				new Option { Value = "Cash" },
				new Option { Value = "Check" }
			};

			var loggedByOptions = new List<Option>
			{
				new Option { Value = "Brian Christensen (Co-Manager)" },
				new Option { Value = "Megan Rueth (Co-Manager)" }
			};

			// Update Player Name to Dropdown (ALL 23 Players)
			if ( playerItemId != null )
			{
				requests.Add( new Request
				{
					UpdateItem = new UpdateItemRequest
					{
						Item = new Item
						{
							ItemId = playerItemId,
							Title = "Player Name",
							QuestionItem = new QuestionItem
							{
								Question = new Question
								{
									Required = true,
									ChoiceQuestion = new ChoiceQuestion
									{
										Type = "DROP_DOWN",
										Options = playerOptions
									}
								}
							}
						},
						Location = new Location { Index = playerItemIndex },
						UpdateMask = "title,questionItem"
					}
				} );
			}

			// Update Amount Collected Options ($75, $37.50, Other)
			if ( amountItemId != null )
			{
				requests.Add( new Request
				{
					UpdateItem = new UpdateItemRequest
					{
						Item = new Item
						{
							ItemId = amountItemId,
							Title = "Amount Collected (USD)",
							QuestionItem = new QuestionItem
							{
								Question = new Question
								{
									Required = true,
									ChoiceQuestion = new ChoiceQuestion
									{
										Type = "RADIO",
										Options = amountOptions
									}
								}
							}
						},
						Location = new Location { Index = amountItemIndex },
						UpdateMask = "title,questionItem"
					}
				} );
			}

			// Update Payment Method Options (Venmo, PayPal, Cash, Check)
			if ( paymentMethodItemId != null )
			{
				requests.Add( new Request
				{
					UpdateItem = new UpdateItemRequest
					{
						Item = new Item
						{
							ItemId = paymentMethodItemId,
							Title = "Payment Method",
							QuestionItem = new QuestionItem
							{
								Question = new Question
								{
									Required = true,
									ChoiceQuestion = new ChoiceQuestion
									{
										Type = "RADIO",
										Options = paymentMethodOptions
									}
								}
							}
						},
						Location = new Location { Index = paymentMethodItemIndex },
						UpdateMask = "title,questionItem"
					}
				} );
			}

			// Update Logged By Options (Brian Christensen & Megan Rueth)
			if ( loggedByItemId != null )
			{
				requests.Add( new Request
				{
					UpdateItem = new UpdateItemRequest
					{
						Item = new Item
						{
							ItemId = loggedByItemId,
							Title = "Logged By (Co-Manager)",
							QuestionItem = new QuestionItem
							{
								Question = new Question
								{
									Required = true,
									ChoiceQuestion = new ChoiceQuestion
									{
										Type = "RADIO",
										Options = loggedByOptions
									}
								}
							}
						},
						Location = new Location { Index = loggedByItemIndex },
						UpdateMask = "title,questionItem"
					}
				} );
			}

			if ( requests.Count > 0 )
			{
				var batchRequest = new BatchUpdateFormRequest { Requests = requests };
				await _formsService.Forms.BatchUpdate( batchRequest, formId ).ExecuteAsync();
				Console.WriteLine( "SUCCESS: Updated $75 Dues Collection Form with ALL 23 players, $75 / $37.50 / Other amounts, PayPal & Megan Rueth!" );
			}
		}

		public async Task SyncUnpaidPlayersToDuesFormAsync( string formId, List<Player> unpaidPlayers )
		{
			await ConfigureDuesFormAsync( formId, unpaidPlayers );
		}

		public async Task AuditAndUpdateFormQuestionsAsync( string formId, string formType )
		{
			if ( _formsService == null )
			{
				await InitializeAsync();
			}

			try
			{
				var currentForm = await _formsService.Forms.Get( formId ).ExecuteAsync();
				Console.WriteLine( "Inspecting Form: " + currentForm.Info.Title + " (ID: " + formId + ")" );
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Form audit note: " + ex.Message );
			}
		}

		public async Task<Form> CreateExpenseFormAsync( string targetFolderId = "1q7vy8NL92cbpIQOI-7BmI0DuLISrX0Yp" )
		{
			if ( _formsService == null || _driveService == null )
			{
				await InitializeAsync();
			}

			var form = new Form
			{
				Info = new Info
				{
					Title = "Oregon JV2 Soccer - Expense & Reimbursement Claim",
					DocumentTitle = "Expense & Reimbursement Claim"
				}
			};

			var createdForm = await _formsService.Forms.Create( form ).ExecuteAsync();
			return await _formsService.Forms.Get( createdForm.FormId ).ExecuteAsync();
		}

		public async Task<Form> CreateDinnerFormAsync( string targetFolderId = "1q7vy8NL92cbpIQOI-7BmI0DuLISrX0Yp" )
		{
			if ( _formsService == null || _driveService == null )
			{
				await InitializeAsync();
			}

			var form = new Form
			{
				Info = new Info
				{
					Title = "Oregon JV2 Soccer - Team Dinner Meal Sign-Up",
					DocumentTitle = "Team Dinner Meal Sign-Up"
				}
			};

			var createdForm = await _formsService.Forms.Create( form ).ExecuteAsync();
			return await _formsService.Forms.Get( createdForm.FormId ).ExecuteAsync();
		}

		public async Task<Form> CreateAndConfigureWaunakeeSubOrderFormAsync( List<Player> rosterPlayers )
		{
			if ( _formsService == null )
			{
				await InitializeAsync();
			}

			Console.WriteLine( "Creating new standalone Google Form for Waunakee Sub Orders..." );
			string formId = null;
			try
			{
				var driveFile = new Google.Apis.Drive.v3.Data.File
				{
					Name = "Oregon JV2 Soccer - Waunakee Tournament Sub Order Form",
					MimeType = "application/vnd.google-apps.form",
					Parents = new List<string> { "1q7vy8NL92cbpIQOI-7BmI0DuLISrX0Yp" }
				};
				var createdDriveFile = await _driveService.Files.Create( driveFile ).ExecuteAsync();
				formId = createdDriveFile.Id;
				Console.WriteLine( "SUCCESS: Created Google Form via Drive API in target folder (Form ID: " + formId + ")" );
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "Drive API create attempt failed: " + ex.Message + ". Attempting Forms API create..." );
				var form = new Form
				{
					Info = new Info
					{
						Title = "Oregon JV2 Soccer - Waunakee Tournament Sub Order Form",
						DocumentTitle = "Waunakee Tournament Sub Order Form"
					}
				};
				var createdForm = await _formsService.Forms.Create( form ).ExecuteAsync();
				formId = createdForm.FormId;
				Console.WriteLine( "SUCCESS: Created Google Form via Forms API (Form ID: " + formId + ")" );
			}

			return await ConfigureWaunakeeSubOrderFormAsync( formId, rosterPlayers );
		}

		public async Task<Form> ConfigureWaunakeeSubOrderFormAsync( string formId, List<Player> rosterPlayers )
		{
			if ( _formsService == null )
			{
				await InitializeAsync();
			}

			Console.WriteLine( "Configuring Waunakee Sub Order Form (ID: " + formId + ")..." );
			var currentForm = await _formsService.Forms.Get( formId ).ExecuteAsync();

			var requests = new List<Request>();

			requests.Add( new Request
			{
				UpdateFormInfo = new UpdateFormInfoRequest
				{
					Info = new Info
					{
						Title = "Oregon JV2 Soccer - Waunakee Tournament Sub Order Form",
						Description = "Select your Jimmy John's sub and chip choice for the Waunakee Tournament."
					},
					UpdateMask = "title,description"
				}
			} );

			if ( currentForm.Items != null && currentForm.Items.Count > 0 )
			{
				for ( int i = currentForm.Items.Count - 1; i >= 0; i-- )
				{
					requests.Add( new Request
					{
						DeleteItem = new DeleteItemRequest
						{
							Location = new Location { Index = i }
						}
					} );
				}
			}

			var playerOptions = rosterPlayers.Select( p => new Option { Value = p.LastName + ", " + p.FirstName + " (Grade " + p.Grade + ")" } ).ToList();

			var sandwichOptions = new List<Option>
			{
				new Option { Value = "Little John #1: The Pepe® (Ham & Provolone)" },
				new Option { Value = "Little John #2: Big John® (Roast Beef)" },
				new Option { Value = "Little John #3: Totally Tuna® (Tuna Salad & Cucumber)" },
				new Option { Value = "Little John #4: Turkey Tom® (Turkey Breast)" },
				new Option { Value = "Little John #5: Vito® (Salami, Capocollo, Provolone, Onion, Oil & Vinegar, Oregano-Basil)" },
				new Option { Value = "Little John #6: The Veggie (Provolone, Avocado Spread, Cucumber)" },
				new Option { Value = "Little John B.L.T. (Bacon, Lettuce, Tomato, Mayo)" }
			};

			var chipOptions = new List<Option>
			{
				new Option { Value = "Regular Jimmy Chips®" },
				new Option { Value = "BBQ Jimmy Chips®" },
				new Option { Value = "Salt & Vinegar Jimmy Chips®" },
				new Option { Value = "Jalapeño Jimmy Chips®" }
			};

			requests.Add( new Request
			{
				CreateItem = new CreateItemRequest
				{
					Item = new Item
					{
						Title = "Email Address (for order confirmation copy)",
						QuestionItem = new QuestionItem
						{
							Question = new Question
							{
								Required = true,
								TextQuestion = new TextQuestion { Paragraph = false }
							}
						}
					},
					Location = new Location { Index = 0 }
				}
			} );

			requests.Add( new Request
			{
				CreateItem = new CreateItemRequest
				{
					Item = new Item
					{
						Title = "Player Name",
						QuestionItem = new QuestionItem
						{
							Question = new Question
							{
								Required = true,
								ChoiceQuestion = new ChoiceQuestion
								{
									Type = "DROP_DOWN",
									Options = playerOptions
								}
							}
						}
					},
					Location = new Location { Index = 1 }
				}
			} );

			requests.Add( new Request
			{
				CreateItem = new CreateItemRequest
				{
					Item = new Item
					{
						Title = "Sandwich Choice",
						QuestionItem = new QuestionItem
						{
							Question = new Question
							{
								Required = true,
								ChoiceQuestion = new ChoiceQuestion
								{
									Type = "DROP_DOWN",
									Options = sandwichOptions
								}
							}
						}
					},
					Location = new Location { Index = 2 }
				}
			} );

			requests.Add( new Request
			{
				CreateItem = new CreateItemRequest
				{
					Item = new Item
					{
						Title = "Chip Choice",
						QuestionItem = new QuestionItem
						{
							Question = new Question
							{
								Required = true,
								ChoiceQuestion = new ChoiceQuestion
								{
									Type = "RADIO",
									Options = chipOptions
								}
							}
						}
					},
					Location = new Location { Index = 3 }
				}
			} );

			requests.Add( new Request
			{
				CreateItem = new CreateItemRequest
				{
					Item = new Item
					{
						Title = "Sandwich Modifiers & Special Instructions",
						Description = "Enter removals or additions (e.g., \"No mayo,\" \"No tomato,\" \"Add mustard,\" \"No onions\").",
						QuestionItem = new QuestionItem
						{
							Question = new Question
							{
								Required = false,
								TextQuestion = new TextQuestion
								{
									Paragraph = true
								}
							}
						}
					},
					Location = new Location { Index = 4 }
				}
			} );

			var batchRequest = new BatchUpdateFormRequest { Requests = requests };
			await _formsService.Forms.BatchUpdate( batchRequest, formId ).ExecuteAsync();

			return await _formsService.Forms.Get( formId ).ExecuteAsync();
		}
	}
}
