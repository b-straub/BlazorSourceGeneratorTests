using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ReactiveProperty
{
    public partial class ReactivePropertyBase : IDisposable
    {
        public IObservable<string> Changed { get => _changed.Where(x => !string.IsNullOrEmpty(x)).AsObservable(); }

        private readonly BehaviorSubject<string> _changed = new BehaviorSubject<string>("");
        private readonly CompositeDisposable _disposeBag = new CompositeDisposable();

        public ReactivePropertyBase()
        {
        }

        public void AddToDisposeBag(IDisposable disposable)
        {
            _disposeBag.Add(disposable);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void NotifyChange(string propertyName)
        {
            _changed.OnNext(propertyName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _changed.Dispose();
                _disposeBag.Dispose();
            }
        }
    }
}
