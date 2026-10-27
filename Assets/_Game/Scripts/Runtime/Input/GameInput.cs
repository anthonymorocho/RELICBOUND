using UnityEngine.InputSystem;

namespace BeforeItWasYou.Input
{
    /// <summary>
    /// Acciones del juego (SISTEMAS_Y_CHECKPOINTS §Controles): WASD, ratón, E, Shift, Ctrl, Tab y Esc,
    /// con equivalentes de mando. Un único conjunto compartido; cada sistema activa el mapa que necesita.
    /// </summary>
    public sealed class GameInput
    {
        private static GameInput _instance;

        public static GameInput Instance => _instance ??= new GameInput();

        public InputActionMap Player { get; }
        public InputAction Move { get; }
        public InputAction Look { get; }
        public InputAction Run { get; }
        public InputAction Crouch { get; }
        public InputAction Interact { get; }
        public InputAction Phone { get; }
        public InputAction Pause { get; }
        public InputAction[] Choices { get; }

        public InputActionMap Ui { get; }
        public InputAction Navigate { get; }
        public InputAction Submit { get; }
        public InputAction Cancel { get; }

        private GameInput()
        {
            Player = new InputActionMap("Player");
            Move = Player.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");
            Look = Player.AddAction("Look", InputActionType.Value);
            Look.AddBinding("<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=12,y=12)");
            Run = Player.AddAction("Run", InputActionType.Button);
            Run.AddBinding("<Keyboard>/leftShift");
            Run.AddBinding("<Gamepad>/leftStickPress");
            Crouch = Player.AddAction("Crouch", InputActionType.Button);
            Crouch.AddBinding("<Keyboard>/leftCtrl");
            Crouch.AddBinding("<Keyboard>/c");
            Crouch.AddBinding("<Gamepad>/buttonEast");
            Interact = Player.AddAction("Interact", InputActionType.Button);
            Interact.AddBinding("<Keyboard>/e");
            Interact.AddBinding("<Gamepad>/buttonSouth");
            Phone = Player.AddAction("Phone", InputActionType.Button);
            Phone.AddBinding("<Keyboard>/tab");
            Phone.AddBinding("<Gamepad>/select");
            Pause = Player.AddAction("Pause", InputActionType.Button);
            Pause.AddBinding("<Keyboard>/escape");
            Pause.AddBinding("<Gamepad>/start");

            Choices = new InputAction[3];
            for (var i = 0; i < Choices.Length; i++)
            {
                Choices[i] = Player.AddAction("Choice" + (i + 1), InputActionType.Button);
                Choices[i].AddBinding("<Keyboard>/" + (i + 1));
                Choices[i].AddBinding("<Keyboard>/numpad" + (i + 1));
            }

            Ui = new InputActionMap("UI");
            Navigate = Ui.AddAction("Navigate", InputActionType.Value);
            Navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Navigate.AddBinding("<Gamepad>/dpad");
            Navigate.AddBinding("<Gamepad>/leftStick");
            Submit = Ui.AddAction("Submit", InputActionType.Button);
            Submit.AddBinding("<Keyboard>/enter");
            Submit.AddBinding("<Keyboard>/numpadEnter");
            Submit.AddBinding("<Keyboard>/space");
            Submit.AddBinding("<Gamepad>/buttonSouth");
            Cancel = Ui.AddAction("Cancel", InputActionType.Button);
            Cancel.AddBinding("<Keyboard>/escape");
            Cancel.AddBinding("<Keyboard>/backspace");
            Cancel.AddBinding("<Gamepad>/buttonEast");
        }

        /// <summary>Activa el control del personaje o el de menús; nunca los dos a la vez.</summary>
        public void UsePlayer(bool player)
        {
            if (player)
            {
                Ui.Disable();
                Player.Enable();
            }
            else
            {
                Player.Disable();
                Ui.Enable();
            }
        }
    }
}
