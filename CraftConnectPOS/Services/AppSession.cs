using CraftConnectPOS.Models;

namespace CraftConnectPOS.Services
{
    public class AppSession : IAppSession
    {
        public UserAccount CurrentUser { get; set; }

        public void Clear()
        {
            CurrentUser = null;
        }
    }
}
