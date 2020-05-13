using System;
using System.Reactive.Linq;
using Microsoft.AspNetCore.Components;
using ReactiveViewModels;

namespace ReactiveComponents.Components
{
    public partial class ReactiveTest : ComponentBase
    {
        [Inject]
        public ReactiveTestViewModel ViewModel { get; set; }

        public ReactiveTest()
        {

        }

        protected override void OnInitialized()
        {
            ViewModel.Changed.Subscribe(x => StateHasChanged());
        }

        private void IncrementCount()
        {
            ViewModel.Count++;
        }

        private void UpdateText()
        {
            ViewModel.Text = $"Reactive count: {ViewModel.Count}!";
        }
    }
}