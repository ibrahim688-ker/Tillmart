namespace CiftlikMarket
{
    /// <summary>Ekranda bir pencere açıkken joystick çalışmasın.</summary>
    public static class UIBlock
    {
        public static bool Active => OfficeUI.IsOpen || OfflineEarnings.IsOpen || AdService.IsShowing || BranchManager.IsConfirmOpen || Tutorial.Blocking
                                    || DailyReward.IsOpen || Quests.IsOpen || Wholesale.IsOpen || DepoStory.Blocking || HireIntro.Blocking;
    }
}
