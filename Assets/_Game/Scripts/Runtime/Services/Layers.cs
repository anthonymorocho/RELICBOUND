namespace BeforeItWasYou
{
    /// <summary>Capas físicas del proyecto (ProjectBootstrap las registra en TagManager).</summary>
    public static class Layers
    {
        public const int Player = 8;
        public const int Interactable = 9;
        public const int Intruder = 10;
        public const int Glass = 11;

        public const int AllButPlayer = ~(1 << Player);
    }
}
