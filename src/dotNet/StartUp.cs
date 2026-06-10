using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using static DesktopPet.StartUp;

#if !PORTABLE
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
#endif

namespace DesktopPet
{
    /// <summary>
    /// StartUp class. This class will initialize the entire application and define some constants.
    /// </summary>
    public sealed class StartUp : IDisposable
    {
        /// <summary>
        /// Maximal sheeps (too much sheeps will cover too much the screen and would not be nice to see).
        /// </summary>
        public const int MAX_SHEEPS = 16;
        private static readonly Random chatRand = new Random();

        public static StartUp Instance { get; private set; }

        /// <summary>
        /// DEBUG TYPE. If you press "SHIFT" by starting the application, a debug Window will appear.
        /// </summary>
        public enum DEBUG_TYPE
        {
            /// <summary>
            /// Only info, to show what is happening.
            /// </summary>
            info = 1,
            /// <summary>
            /// Something important happened or something that was not expected.
            /// </summary>
            warning = 2,
            /// <summary>
            /// An error is occurred. The application need to do something that was not expected.
            /// </summary>
            error = 3,
        }

        /// <summary>
        /// A timer to allow some times to the sheeps to die, before the application will close definitively.
        /// </summary>
        static public System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();

        /// <summary>
        /// Each sheep is in a different form.
        /// </summary>
        readonly FormPet[] sheeps = new FormPet[MAX_SHEEPS];
        private TriggerEngine triggerEngine;
        private PerformanceHelper perfHelper;
        private OllamaService ollamaSvc;
        internal LocalModelManager ModelManager;

        /// <summary>
        /// Debug window, used only if SHIFT was pressed by starting the application.
        /// </summary>
        static FormDebug debug = null;

        /// <summary>
        /// Number of currently active sheeps.
        /// </summary>
        int iSheeps = 0;

        /// <summary>
        /// The XML file where all animations are defined.
        /// </summary>
        Xml xml;

        /// <summary>
        /// Class of the animations. All animations are stored there.
        /// </summary>
        Animations animations;

        /// <summary>
        /// Process Icon. The tray icon on the taskbar.
        /// </summary>
        readonly ProcessIcon pi;

        bool isRealoadingSettings = false;
        
        /// <summary>
        /// Error message for exceptions. It is shown in the options if an error occurs.
        /// </summary>
        public struct TError
        {
                /// <summary>
                /// The message to show in the option dialog.
                /// </summary>
            public string AudioErrorMessage;
        }

        /// <summary>
        /// Error messages (used for debug), visible in the option dialog.
        /// </summary>
        public TError ErrorMessages;
        
        /// <summary>
        /// Constructor. Called when application is started.
        /// </summary>
        /// <param name="processIcon">ProcessIcon class, to change icon when a new pet is selected.</param>
        public StartUp(ProcessIcon processIcon)
        {
            Instance = this;
            pi = processIcon;
                        
                // Init XML class
            xml = new Xml((int)Math.Pow(2, Program.MyData.GetScale() - 1));
                // Init Animations class
            animations = new Animations(xml);

                // If SHIFT key was pressed, open Debug window
            Keys ks = Control.ModifierKeys;
            if (ks == Keys.Shift)
            {
                debug = new FormDebug();
                debug.Show();
                AddDebugInfo(DEBUG_TYPE.info, "debug window started");
            }
            
                // Read XML file and start new sheep in 1 second
            if(!xml.ReadXML())
            {
                Program.MyData.SetXml(Properties.Resources.animations, "esheep64");
                xml.ReadXML();
            }

                // Set animation icon
            pi.SetIcon(xml.bitmapIcon, 
                        xml.AnimationXML.Header.Petname, 
                        xml.AnimationXML.Header.Author, 
                        xml.AnimationXML.Header.Title, 
                        xml.AnimationXML.Header.Version, 
                        xml.AnimationXML.Header.Info
                        );

                // Wait 1 second, before starting first animation
            timer1.Tag = "A";
            timer1.Tick += new EventHandler(Timer1_Tick);
            timer1.Interval = 1000;
            timer1.Enabled = true;

            Program.MyData.ListenOnXMLChanged(XmlFileChanged);
            Program.MyData.ListenOnOptionsChanged(OptionFileChanged);

            perfHelper = new PerformanceHelper();
            ollamaSvc = new OllamaService();
            InitializeTriggerEngine();

            ModelManager = new LocalModelManager();
            ModelManager.StatusChanged += () =>
            {
                StartUp.AddDebugInfo(DEBUG_TYPE.info, "Model: " + ModelManager.Status);
                if (ModelManager.IsReady)
                {
                    ConfigureOllama();
                    if (triggerEngine != null && Program.MyData.GetOllamaEnabled())
                        RestartTriggerEngine();
                }
            };
            _ = InitModelAsync();
        }

        private async Task InitModelAsync()
        {
            await ModelManager.EnsureModelReadyAsync();
            AddDebugInfo(DEBUG_TYPE.info, "Local model: " + ModelManager.Status);
        }

        private void InitializeTriggerEngine()
        {
            if (ollamaSvc != null) ollamaSvc.SyncFromConfig();
            if (perfHelper == null) perfHelper = new PerformanceHelper();

            triggerEngine = new TriggerEngine(ollamaSvc, perfHelper);
            triggerEngine.OnMessageGenerated += (msg) =>
            {
                if (iSheeps == 0) return;
                var pet = sheeps[chatRand.Next(iSheeps)];
                if (pet != null && !pet.IsDisposed)
                {
                    pet.ShowBubble(msg);
                }
            };
            triggerEngine.Start();
        }

        public void RestartTriggerEngine()
        {
            triggerEngine?.Stop();
            InitializeTriggerEngine();
        }


        private void XmlFileChanged(object source, FileSystemEventArgs e)
        {
            Thread.Sleep(200);
            Program.MyData.LoadXML();
            Program.Mainthread.LoadNewXMLFromString(Program.MyData.GetXml());
        }

        private void OptionFileChanged(object source, FileSystemEventArgs e)
        {
            if (isRealoadingSettings) return;
            isRealoadingSettings = true;
            Thread.Sleep(1000);
            Program.MyData.LoadSettings();
            Thread.Sleep(200);
            isRealoadingSettings = false;
        }

        /// <summary>
        /// Dispose class -> used to dispose xml class
        /// </summary>
        public void Dispose()
        {
            xml.Dispose();
            pi.Dispose();
        }
        
            /// <summary>
            /// Calling this function will add another sheep on the desktop, if MAX_SHEEP was not reached.
            /// </summary>
        public void AddSheep()
        {
            if (iSheeps < MAX_SHEEPS)
            {
                var newSheep = new FormPet(animations, xml);
                newSheep.PetIndex = iSheeps;
                foreach (var sprite in xml.sprites)
                {
                    newSheep.AddImage(sprite);
                }
                sheeps[iSheeps] = newSheep;
                sheeps[iSheeps].Show(xml.spriteWidth, xml.spriteHeight);
                AddDebugInfo(DEBUG_TYPE.info, "new pet...");
                AddDebugInfo(DEBUG_TYPE.info, xml.sprites.Count.ToString() + " frames added");
                
                    // Start the animation of the pet
                sheeps[iSheeps].Play(true);
                ConfigureOllama();
                iSheeps++;
            }
            else
            {
                AddDebugInfo(DEBUG_TYPE.warning, "max PETs reached");
            }
        }

        public void ConfigureOllama()
        {
            var ep = Program.MyData.GetOllamaEndpoint();
            var mod = Program.MyData.GetOllamaModel();
            var sp = Program.MyData.GetOllamaSystemPrompt();
            var en = Program.MyData.GetOllamaEnabled();
            OllamaClient.Configure(ep, mod, sp, en);

            if (ollamaSvc != null)
            {
                ollamaSvc.Endpoint = ep;
                ollamaSvc.ModelName = mod;
                ollamaSvc.SystemPrompt = sp;
            }
            if (en)
            {
                RestartTriggerEngine();
            }
            else
            {
                triggerEngine?.Stop();
            }
        }

        public FormPet GetRandomPet(FormPet exclude)
        {
            if (iSheeps <= 1) return null;
            var candidates = new List<FormPet>();
            for (int i = 0; i < iSheeps; i++)
            {
                if (sheeps[i] != null && sheeps[i] != exclude && !sheeps[i].IsDisposed)
                    candidates.Add(sheeps[i]);
            }
            if (candidates.Count == 0) return null;
            return candidates[chatRand.Next(candidates.Count)];
        }

        public void BroadcastMessage(FormPet sender, string message)
        {
            if (iSheeps < 2) return;
            var target = GetRandomPet(sender);
            if (target == null) return;

            var targetName = target.PetName;
            var senderName = sender.PetName;
            string prompt = $"{senderName} dijo: \"{message}\". {targetName}, responde como mascota virtual de forma corta y graciosa.";

            System.Windows.Forms.Timer responseTimer = new System.Windows.Forms.Timer();
            responseTimer.Interval = 3000 + chatRand.Next(3000);
            responseTimer.Tick += (s, e) =>
            {
                responseTimer.Stop();
                responseTimer.Dispose();
                if (!target.IsDisposed)
                {
                    target.TriggerAI(prompt);
                }
            };
            responseTimer.Start();
        }

            /// <summary>
            /// Close all sheeps on the desktop and eventually closes the application.
            /// </summary>
            /// <param name="exit">If true, the application will close after 1 second (leaving time to the sheeps to die).</param>
        public void KillSheeps(bool exit)
        {
            AddDebugInfo(DEBUG_TYPE.info, "Killing all sheeps");
            timer1.Tag = "0";
            pi.Dispose();

                // Leave open application to show some kill animations. 
                // Only if there is a pet on the desktop.
            if (iSheeps > 0)
            {
                Random rand = new Random();
                for (int i = 0; i < iSheeps; i++)
                {
                    Thread.Sleep(rand.Next(100, 200));
                    sheeps[i].Kill();
                    Application.DoEvents();
                }
                iSheeps = 0;

                if (exit)
                {
                    timer1.Interval = 1100;
                    timer1.Enabled = true;
                }
            }
            else
            {
                timer1.Interval = 100;
                timer1.Enabled = true;
            }
        }


        /// <summary>
        /// Bring every sheep to top most again
        /// </summary>
        public void TopMostSheeps()
        {
            AddDebugInfo(DEBUG_TYPE.info, "Top most all sheeps");

            for (int i = 0; i < iSheeps; i++)
            {
                sheeps[i].TopMost = true;
            }
        }

        /// <summary>
        /// Close a single sheep on the desktop.
        /// </summary>
        /// <param name="sheep">The sheep-form to close.</param>
        public bool KillSheep(FormPet sheep)
        {
            bool bSheepRemoved = false;

            AddDebugInfo(DEBUG_TYPE.info, "Kill one sheep");
            
            for (int i = 0; i < iSheeps; i++)
            {
                if(sheeps[i] == sheep)
                {
                    sheeps[i].Kill();
                    for (int j = i; j < iSheeps - 1; j++) sheeps[j] = sheeps[j + 1];
                    iSheeps--;
                    Application.DoEvents();
                    bSheepRemoved = true;
                    break;
                }
            }

            /*
             * This will close application if all Sheeps are removed. But Maybe the user want see the try icon to add a sheep later.
             * Maybe in future you can choose 0 to 10 sheeps at startup, so this is commented out for the moment.
            if (iSheeps <= 0)
            {
                timer1.Tag = "0";
                pi.Dispose();

                timer1.Interval = 1100;
                timer1.Enabled = true;
            }
            */

            return bSheepRemoved;
        }

            /// <summary>
            /// Timer used to init and close the application.
            /// </summary>
            /// <param name="sender">Caller as object.</param>
            /// <param name="e">Timer event values.</param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
                // "A" when application starts. Add a sheep.
            if (timer1.Tag.ToString() == "A")
            {
				if (iSheeps < Program.MyData.GetAutoStartPets() && iSheeps < MAX_SHEEPS)
				{
					if (iSheeps == 0)
					{
						AddDebugInfo(DEBUG_TYPE.info, "init application...");
						xml.LoadAnimations(animations);
					}

					AddSheep();
				}
				else
				{
					timer1.Enabled = false;
					timer1.Tag = "B";
				}
            }
                // "0" when application should be stopped.
            else if (timer1.Tag.ToString() == "0")
            {
                timer1.Tag = "1";
            }
            else
            {
                Application.Exit();
            }
        }
        
            /// <summary>
            /// Load new XML (from XML string).
            /// </summary>
            /// <param name="strXml">A string with the xml content.</param>
        public void LoadNewXMLFromString(string strXml)
        {
            AddDebugInfo(DEBUG_TYPE.info, "load new XML string");

            if (sheeps[0].InvokeRequired)
            {
                sheeps[0].BeginInvoke(new MethodInvoker(delegate{
                    LoadNewXMLFromString(strXml);
                }));
                return;
            }

            // Close all sheeps
            for (int i = 0; i < iSheeps; i++)
            {
                sheeps[i].Kill();
                /*
                sheeps[i].Close();
                sheeps[i].Dispose();
                */
            }
            iSheeps = 0;

                // reload XML and Animations
            xml = new Xml(Program.MyData.GetScale());
            animations = new Animations(xml);
                        
            if (!xml.ReadXML())
            {
                Program.MyData.SetXml(Properties.Resources.animations, "esheep64");
                xml.ReadXML();
            }

            pi.SetIcon(
                xml.bitmapIcon, 
                xml.AnimationXML.Header.Petname, 
                xml.AnimationXML.Header.Author, 
                xml.AnimationXML.Header.Title, 
                xml.AnimationXML.Header.Version, 
                xml.AnimationXML.Header.Info);

                // start animation in 1 second.
            timer1.Tag = "A";
            timer1.Enabled = true;
        }

            /// <summary>
            /// Returns the Animation class.
            /// </summary>
            /// <returns>Member variable to access all animations of the current pet.</returns>
        public Animations GetAnimations()
        {
            return animations;
        }

            /// <summary>
            /// If the application is started with the SHIFT key pressed, warnings and errors are reported on a window.
            /// </summary>
            /// <param name="type">See <see cref="StartUp.DEBUG_TYPE"/> for the possible values. </param>
            /// <param name="text">Text to show in the dialog window.</param>
        public static void AddDebugInfo(DEBUG_TYPE type, string text)
        {
            if(debug != null)
            {
                if (debug.InvokeRequired)
                {
                    debug.BeginInvoke(new MethodInvoker(delegate {
                        debug.AddDebugInfo(type, text);
                    }));
                }
                else
                {
                    debug.AddDebugInfo(type, text);
                }
            }
        }

            /// <summary>
            /// If the application is started with the SHIFT key pressed, some extra features are activated.
            /// </summary>
            /// <returns>true if the application is running with debug window.</returns>
        public static bool IsDebugActive()
        {
            return (debug != null);
        }

        /// <summary>
        /// Calling this function, all sheeps will execute the same animation (if the sync-word is present in the XML).
        /// </summary>
        public void SyncSheeps()
        {
            AddDebugInfo(DEBUG_TYPE.info, "synchronize sheeps");
            for (int i = 0; i < iSheeps; i++)
            {
                sheeps[i].Sync();
            }
        }
    }
}
