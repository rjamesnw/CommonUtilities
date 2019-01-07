using System;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Messaging;
using System.Windows.Shapes;
using Common;
using Common.Data;

namespace Common.Silverlight.Communication
{
    // #####################################################################################################################

    public class InterapplicationCommand
    {
        public InterapplicationCommunicationsManager InterappCommManager
        { get { return _InterappCommManager; } set { _InterappCommManager = value ?? _InterappCommManager; } }
        InterapplicationCommunicationsManager _InterappCommManager;

        public string Command;
        public string[] Arguments;
        public Exception Error;
        public bool Cancelled;
        public string Response; // (the response to the command)

        public void SendCommandReply(string command, params string[] args)
        { Response = InterappCommManager.CreateSendCommandString(command, args); }
    }

    public class InterapplicationCommunicationsManager
    {
        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Triggered when a non-invoke command is received.
        /// </summary>
        public event Action<InterapplicationCommand> CommandReceived;

        // ---------------------------------------------------------------------------------------------------------------------

        public readonly LocalMessageSender ApplicationMessageSender;
        public readonly LocalMessageReceiver ApplicationMessageReceiver;

        /// <summary>
        /// Will be true if listing was successful, and false if another instance is already listening.
        /// </summary>
        public bool IsListening { get; private set; }

        // ---------------------------------------------------------------------------------------------------------------------

        string _SecureCommunicationKey = "F1ED4A87089F4A7484926377E27C86A9"; // (random GUID to use as a base)

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance for intercommunication between applications.
        /// </summary>
        /// <param name="receiverName">A name that uniquely identifies this communication instance. The name acts like a 'port' between these instances. The default is an internal identifier.</param>
        /// <param name="hashKey">A key used to further encrypt interapplication communication (not required). Must be non-null, and not whitespace.</param>
        public InterapplicationCommunicationsManager(string receiverName = null, string hashKey = null)
        {
            if (!string.IsNullOrWhiteSpace(hashKey))
                _SecureCommunicationKey = Encryption.ToBase64UTF8(Encryption.XORStringWithKey(_SecureCommunicationKey, receiverName + hashKey));

            if (string.IsNullOrWhiteSpace(receiverName))
                receiverName = _SecureCommunicationKey;

            ApplicationMessageSender = new LocalMessageSender(receiverName, LocalMessageSender.Global);
#if DEBUG
            ApplicationMessageReceiver = new LocalMessageReceiver(receiverName, ReceiverNameScope.Global, LocalMessageReceiver.AnyDomain);
#else
            ApplicationMessageReceiver = new LocalMessageReceiver(receiverName, ReceiverNameScope.Global, new string[] { "jamonjoo.com", "127.0.0.1", "localhost" });
#endif

            // ... setup the controller's intercommunication system ...

            ApplicationMessageSender.SendCompleted += _ICM_SendCompleted;
            ApplicationMessageReceiver.MessageReceived += _ICM_MessageReceived;

            try
            {
                ApplicationMessageReceiver.Listen();
                IsListening = true;
            }
            catch { }
        }

        // ---------------------------------------------------------------------------------------------------------------------

        void _ICM_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var cmd = _ProcessMessage(e.Message, null, false);

            if (cmd != null && cmd.Response != null)
                e.Response = cmd.Response;
        }

        private InterapplicationCommand _ProcessMessage(string message, Exception error, bool cancelled)
        {
            // ... validate message and get "Header info" ...

            if (string.IsNullOrWhiteSpace(message)) return null;

            string[] messageParts = message.Split(',');
            if (messageParts.Length < 2) return null; // (invalid message! - requires at least TWO items 'secure_app_key,command[,args...]')

            string secureKey = messageParts[0];
            if (secureKey != _SecureCommunicationKey) return null; // (not related to this instance family)

            string command = messageParts[1];

            // ... move the variable arguments to a new array (this allows the "header info" to be updated without
            // affecting command argument validations) ...

            var arguments = new string[messageParts.Length - 2]; // (Note: These are COMMAND arguments, NOT "Invoke" arguments)
            if (arguments.Length > 0)
                Array.Copy(messageParts, 2, arguments, 0, arguments.Length);

            // ... process command ...

            if (command == "Invoke") // (Expected arguments: full declaring type name, method name[, arg type 1, value 1, arg type 2, value 2, etc...])
            {
                if (arguments.Length < 2) return null; // (invalid invoke request - at least 2 command arguments needed!)
                string declaringTypeName = arguments[0];
                string methodName = arguments[1];

                // ... get the declaring type and method information ...

                Type declaringType = Type.GetType(declaringTypeName);
                if (declaringType != null)
                {
                    // ... the method signature depends on the arguments, so get them next (in [type,value] pairs) ...

                    if (arguments.Length % 2 != 0) return null; // (invalid number of arguments [should be in pairs])

                    int numberOfArgs = (arguments.Length - 2) / 2;

                    Type[] types = new Type[numberOfArgs];
                    object[] values = new object[numberOfArgs];

                    for (int i = 0; i < numberOfArgs; i++)
                    {
                        var type = Type.GetType(arguments[i * 2]);

                        if (type == null) return null; // (ERROR! Invalid/Unknown type, abort...)

                        object value = ((string)arguments[i * 2 + 1]).Trim().Replace("{COMMA}", ",");
                        var strValue = value.ND();

                        if (type == typeof(String))
                        {
                            if (strValue.StartsWith("{") && strValue.EndsWith("}"))
                                value = strValue.Substring(1, strValue.Length - 2);
                            else
                                return null; // (invalid string entry! This message part MUST be either empty (==null), or placed between curly braces (i.e. {Some text}, where {} == "").
                        }
                        else // ... not a string, so convert the string value back into its proper type ...
                        {
                            if (value == null || string.IsNullOrEmpty(strValue))
                                value = null;

                            try { value = Types.ChangeType(value, type); }
                            catch { return null; /* Invalid conversion! Abort... */ }
                        }

                        types[i] = type;
                        values[i] = value;
                    }

                    MethodInfo methodInfo = declaringType.GetMethod(methodName, types);
                    if (methodInfo == null) return null; // (method signature not found!)

                    var result = methodInfo.Invoke(null, values); // TODO: Return result somehow?
                }
            }
            else
            {
                if (CommandReceived != null)
                {
                    var cmd = new InterapplicationCommand
                    {
                        InterappCommManager = this,
                        Command = command,
                        Arguments = arguments,
                        Error = error,
                        Cancelled = cancelled
                    };

                    CommandReceived(cmd);

                    return cmd;
                }

            }

            return null;
        }

        void _ICM_SendCompleted(object sender, SendCompletedEventArgs e)
        {
            var cmd = e.Error == null ? _ProcessMessage(e.Response, e.Error, e.Cancelled) : new InterapplicationCommand { InterappCommManager = this, Error = e.Error };

            if (e.UserState is Action<InterapplicationCommand>)
                ((Action<InterapplicationCommand>)e.UserState)(cmd);

            if (cmd != null && cmd.Response != null)
                ApplicationMessageSender.SendAsync(cmd.Response, e.UserState);
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Invokes the specified static method remotely on ALL listening instances of SilverWindows that belong to the
        /// same AppGUID family.
        /// </summary>
        /// <param name="methodDeclaringType">The class or struct which contains the static method.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="callBack">Any method to execute on success OR failure.</param>
        /// <param name="args">The arguments to pass onto the static method.</param>
        void _InvokeRemotely(Type methodDeclaringType, string methodName, Action<SendCompletedEventArgs> callBack, params object[] args)
        {
            if (methodDeclaringType == null) throw new ArgumentNullException("methodDeclaringType");
            if (string.IsNullOrEmpty(methodName)) throw new ArgumentNullException("methodName", "Cannot be 'null' OR empty.");

            string message = _SecureCommunicationKey + ",Invoke," + methodDeclaringType.FullName + ",";
            message += methodName;

            if (args != null && args.Length > 0)
            {
                foreach (object arg in args)
                {
                    var argType = arg.GetType();

                    message += "," + argType.GetType().FullName + ",";

                    if (arg != null)
                    {
                        if (argType == typeof(string))
                            message += '{' + arg.ToString().Replace(",", "{COMMA}") + '}'; // (i.e. {Some{COMMA}text} or {} where {} == "")
                        else
                            message += arg.ToString().Replace(",", "{COMMA}");
                    }
                }
            }

            ApplicationMessageSender.SendAsync(message, callBack);
        }

        // ---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Send an internal command remotely between all family instances.
        /// </summary>
        public void SendCommand(Action<InterapplicationCommand> callBack, string command, params string[] args)
        {
            if (IsListening)
                throw new InvalidOperationException("SendCommand(...,'" + command + "',...): Cannot send commands to self.");

            string message = CreateSendCommandString(command, args);

            ApplicationMessageSender.SendAsync(message, callBack);
        }

        public string CreateSendCommandString(string command, params string[] args)
        {
            string message = _SecureCommunicationKey + "," + command;

            if (args != null && args.Length > 0)
                foreach (var s in args)
                    message += "," + s;

            return message;
        }

        /// <summary>
        /// Send an internal command remotely between all family instances.
        /// </summary>
        public void SendCommand(string command, params string[] args)
        { SendCommand((Action<InterapplicationCommand>)null, command, args); }

        // ---------------------------------------------------------------------------------------------------------------------
    }

    // #####################################################################################################################
}
