using System;
using System.Threading.Tasks;

namespace Modio.Unity.UI.Panels
{
    public abstract class ModioWaitingPanelBase : ModioPanelBase
    {
        public async void OpenAndWaitFor<T>(Task<T> task, Action<T> action)
        {
            OpenPanel();

            await task;

            ClosePanel();

            action(task.Result);
        }

        public async Task<T> OpenAndWaitForAsync<T>(Task<T> task)
        {
            OpenPanel();

            await task;

            ClosePanel();

            return task.Result;
        }

        public async Task OpenAndWaitFor(Task task, Action action = null)
        {
            OpenPanel();

            await task;

            ClosePanel();

            action?.Invoke();
        }

        public override void DoDefaultSelection()
        {
            //Clears the selection when the panel opens and prevents the fallback selection from happening afterwards
            SetSelectedGameObject(null);
        }

        protected override void CancelPressed()
        {
            //Don't allow the user to close the waiting panel
            //base.CancelPressed();
        }
    }
}
