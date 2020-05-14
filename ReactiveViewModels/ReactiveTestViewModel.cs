using System;
using System.Reactive.Linq;
using ReactiveProperty;

namespace ReactiveViewModels
{
    public partial class ReactiveTestViewModel : IReactiveProperty
    {
        private const string _answer = "Answer to the Ultimate Question of Life, the Universe, and Everything";
        private const string _button = @"Push ""Update Text"" to update";

        [ReactiveProperty(PropertyName = "Text")]
        private string _textField = _button;

        [ReactiveProperty(PropertyName = "Count")]
        private int _countField = 40;

        public ReactiveTestViewModel()
        {
            AddToDisposeBag(Changed
                .Where(p => p == "Count")
                .Select(_ => _countField)
                .Subscribe(c =>
                {
                    if (c == 42)
                    {
                        _textField = _answer;
                    }
                    else
                    {
                        _textField = _button;
                    }
                }));
        }
    }
}