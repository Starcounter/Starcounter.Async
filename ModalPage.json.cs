using System.Threading.Tasks;
using Nito.AsyncEx;
using Starcounter;
using Starcounter.Authorization.Routing;
using Starcounter.Templates;

namespace StarcounterApplication4
{
    [Url("/modal")]
    public partial class ModalPage : Json, IInitPage
    {
        public bool ShowMain => !ShowNameDialog && !ShowConfirmationDialog;

        public async void Handle(Input.ChangeFirstNameTrigger input)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSynchronizationContext()))
            {
                var newFirstName = await DisplayNameDialogAsync(FirstName);
                FirstName = newFirstName;
            }
        }

        public async void Handle(Input.ChangeLastNameTrigger input)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSynchronizationContext()))
            {
                var newLastName = await DisplayNameDialogAsync(LastName);
                LastName = newLastName;
            }
        }

        public async void Handle(Input.ResetNameTrigger input)
        {
            using (new SynchronizationContextSwitcher(new StarcounterSynchronizationContext()))
            {
                if (await ShowConfirmationDialogAsync())
                {
                    ResetName();
                }
            }
        }

        private async Task<bool> ShowConfirmationDialogAsync()
        {
            ShowConfirmationDialog = true;
            try
            {
                ConfirmTrigger = CancelTrigger = 0;
                await Task.WhenAny(ChangeOfProperty(Template.ConfirmTrigger), ChangeOfProperty(Template.CancelTrigger));
                return ConfirmTrigger == 1;
            }
            finally
            {
                ShowConfirmationDialog = false;
            }
        }

        private async Task<string> DisplayNameDialogAsync(string oldName)
        {
            ShowNameDialog = true;
            DialogName = oldName;
            try
            {
                return await ChangeOfProperty(Template.DialogName);
            }
            finally
            {
                ShowNameDialog = false;
            }
        }

        /// <summary>
        /// Returns a task which will complete when <paramref name="property"/> is changed.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static Task<string> ChangeOfProperty(TString property)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();
            property.AddHandler((page, prop, value) => new Input.DialogName { App = (ModalPage)page, Template = (TString)prop, Value = value },
                (page, input) => { taskCompletionSource.TrySetResult(input.Value); });
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Returns a task which will complete when <paramref name="property"/> is changed.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static Task<long> ChangeOfProperty(TLong property)
        {
            var taskCompletionSource = new TaskCompletionSource<long>();
            property.AddHandler((page, prop, value) => new Input.CancelTrigger { App = (ModalPage)page, Template = (TLong)prop, Value = value },
                (page, input) => { taskCompletionSource.TrySetResult(input.Value); });
            return taskCompletionSource.Task;
        }

        public void Init()
        {
            ResetName();
        }

        private void ResetName()
        {
            FirstName = "John";
            LastName = "Doe";
        }
    }
}
