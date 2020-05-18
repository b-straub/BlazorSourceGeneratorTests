using System;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components;
using ReactiveViewModels;

namespace ReactiveComponents.Components
{
    public partial class ReactiveTest : ComponentBase
    {
        [Inject]
        public ReactiveTestViewModel ViewModel { get; set; }

        private ReactiveViewProxy _viewProxy;


        protected override void OnInitialized()
        {
            _viewProxy = new ReactiveViewProxy(ViewModel, StateHasChanged);
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