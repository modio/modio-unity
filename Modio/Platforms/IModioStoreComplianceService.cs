using System.Threading.Tasks;

namespace Modio.Platforms
{
    public interface IModioStoreComplianceService
    {
        public Task ShowNoPacksError();

        public void ShowStoreInformation();

        public void HideStoreInformation();
    }
}
