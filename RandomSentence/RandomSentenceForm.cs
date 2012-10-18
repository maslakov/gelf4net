using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;

namespace SandboxWindow
{
    public partial class RadomSentenceForm : Form
    {
        protected static readonly ILog log = LogManager.GetLogger( typeof( RadomSentenceForm ) );
        private static Boolean isRun;
        private TaskScheduler scheduler;
        private CancellationTokenSource tokensource;
        private CancellationToken token;
        private Object rootLock = new Object();
        private Task maintask;
        private Random rand = new Random(unchecked((Int32)DateTime.Now.Ticks));

        public RadomSentenceForm()
        {
            log4net.Config.XmlConfigurator.Configure();
            InitializeComponent();
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        private void Form1_Load( Object sender, EventArgs e )
        {
            log.Debug( "Starting the randomizer sentece" );
            label1.Text = RandomizeSentence();
        }

        private String RandomizeSentence()
        {
            ThreadContext.Properties["requestId"] = Guid.NewGuid().ToString();

            var syntaxes = File.ReadAllLines( @"Resources\syntax.txt" );
            var nouns = File.ReadAllLines( @"Resources\nouns.txt" );
            var verbs = File.ReadAllLines( @"Resources\verbs.txt" );
            var adjectives = File.ReadAllLines( @"Resources\adjectives.txt" );
            var adverbs = File.ReadAllLines( @"Resources\adverbs.txt" );
            

            var sentence = syntaxes[rand.Next( syntaxes.Length )];
            var complete = false;
            while( !complete )
            {
                var index = sentence.IndexOf( "[Noun]" );
                if( index > 0 )
                {
                    sentence = sentence.Remove( index, "[Noun]".Length );
                    sentence = sentence.Insert( index, nouns[rand.Next( nouns.Length )].Trim() );
                    continue;
                }
                index = sentence.IndexOf( "[Verb]" );
                if( index > 0 )
                {
                    sentence = sentence.Remove( index, "[Verb]".Length );
                    sentence = sentence.Insert( index, verbs[rand.Next( verbs.Length )].Trim() );
                    continue;
                }
                index = sentence.IndexOf( "[Adverb]" );
                if( index > 0 )
                {
                    sentence = sentence.Remove( index, "[Adverb]".Length );
                    sentence = sentence.Insert( index, adverbs[rand.Next( adverbs.Length )].Trim() );
                    continue;
                }
                index = sentence.IndexOf( "[Adjective]" );
                if( index > 0 )
                {
                    sentence = sentence.Remove( index, "[Adjective]".Length );
                    sentence = sentence.Insert( index, adjectives[rand.Next( adjectives.Length )].Trim() );
                    continue;
                }
                complete = true;
            }
            var cutIndex = 0;
            var readChars = 0;
            const Int32 maxWidth = 32;
            while( sentence.Length > readChars + maxWidth )
            {
                cutIndex = sentence.Substring( cutIndex, maxWidth ).LastIndexOf( " " );
                sentence = sentence.Remove( readChars + cutIndex, 1 );
                sentence = sentence.Insert( readChars + cutIndex, "\n" );
                readChars += cutIndex;
            }
            var result = sentence.ToUpper();
            log.Debug( String.Format( "Randomizer Sentence: {0}", result ) );
            return result;
        }

        private void Form1_Click( Object sender, EventArgs e )
        {
            lock( rootLock )
            {
                if( !isRun )
                {
                    tokensource = new CancellationTokenSource();
                    token = tokensource.Token;

                    this.Text = "Running...";
                    log.Info( "Start running..." );
                    var task = Task.Factory.StartNew(
                        () =>
                        {
                            while( ! token.IsCancellationRequested )
                            {
                                RandomizeSentence();
                            }
                        }, token );

                    maintask = task.ContinueWith(
                            finished =>
                            {
                                if( finished.Exception != null )
                                {
                                    log.Error( finished.Exception.Flatten() );
                                }
                            } );
                    isRun = true;
                }
                else
                {
                    tokensource.Cancel();
                    this.Text = "Waiting...";
                    log.Info("Waiting for stop...");
                    
                    maintask.Wait();
                    
                    log.Info("Stopped...");
                    isRun = false;
                }
            }
        }

        private void Form1_FormClosing( Object sender, FormClosingEventArgs e )
        {
            log.Debug( "Closing the app" );
        }
    }
}