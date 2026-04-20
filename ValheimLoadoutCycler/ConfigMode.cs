namespace ValheimLoadoutCycler
{
    public static class ConfigMode
    {
        public static bool IsActive { get; private set; }
        public static int EditingLoadoutIndex { get; set; }

        public static void Toggle()
        {
            if (IsActive)
                Exit();
            else
                Enter();
        }

        public static void Enter()
        {
            IsActive = true;
            EditingLoadoutIndex = LoadoutManager.ActiveIndex;
        }

        public static void Exit()
        {
            IsActive = false;
            LoadoutManager.Save();
        }

        public static void ExitOnInventoryClose()
        {
            if (IsActive)
                Exit();
        }
    }
}
