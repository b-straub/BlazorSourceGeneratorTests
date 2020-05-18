using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveViewModels
{
    public partial class ReactiveViewProxy
    {
        public string Text { get; private set; }
        public int Number { get; private set; }

        private void TestAction(string value)
        {
            Text = $"Reactive test action: {value}!";
        }
        /*private void TestAction(string text, int? count)
        {
            Text = $"Reactive test action: Text: {text ?? "N/A"}, Count: {count ?? -1}!";
        }*/
    }

    public partial class ReactiveViewProxy
    {
        public ReactiveViewProxy(ReactiveTestViewModel viewModel, Action stateChange)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            viewModel.RegisterReactiveAction(stateChange);
            viewModel.RegisterReactiveAction(TestAction, nameof(viewModel.Text));
            /*viewModel.RegisterReactiveAction(TestAction, new string[] { nameof(viewModel.Text), nameof(viewModel.Count) });

            viewModel.RegisterReactiveProperty(this, x => x.Text, x => x.Text);
            viewModel.RegisterReactiveProperty(this, x => x.Number, x => x.Count);*/
        }
    }
}
