namespace CiftlikMarket
{
    /// <summary>
    /// Şubeye özel kayıt anahtarları. Merkez (1. şube) eski anahtarları aynen kullanır (eski kayıtlar bozulmaz);
    /// diğer şubeler "b2_", "b3_" önekiyle ayrılır. Para ("cm_money") ve "cmg_" anahtarları ortaktır.
    /// </summary>
    public static class SaveKeys
    {
        public static string P(string key) => For(BranchManager.Branch, key);
        public static string For(int branch, string key) => branch <= 1 ? key : "b" + branch + "_" + key;
    }
}
