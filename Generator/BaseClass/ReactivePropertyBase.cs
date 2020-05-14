using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ReactiveProperty
{
    /// <summary>Interface for using ReactiveProperty attribute(s).</summary>
    public interface IReactiveProperty : IDisposable
    {
        /// <summary>This observable emits the name
        ///    of any changed property.
        /// <example>For example:
        /// <code>
        ///     AddToDisposeBag(Changed.Where(pn => pn == "PropertyName").Subscribe(_ => ...));
        /// </code>
        /// </example>
        /// </summary>

        public IObservable<string> Changed { get; }

        /// <summary>This method can be used
        ///    disposing any subscription made by the ViewModel.
        /// <example>For example:
        /// <code>
        ///     AddToDisposeBag(Changed.Where(pn => pn == "PropertyName").Subscribe(_ => ...));
        /// </code>
        /// </example>
        /// </summary>
        public void AddToDisposeBag(IDisposable disposable);
    }

    public class ReactivePropertyBaseImpl : IReactiveProperty
    {
        public IObservable<string> Changed { get => _changed.Where(x => !string.IsNullOrEmpty(x)).AsObservable(); }

        private readonly BehaviorSubject<string> _changed = new BehaviorSubject<string>("");
        private readonly CompositeDisposable _disposeBag = new CompositeDisposable();

        public ReactivePropertyBaseImpl()
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

        public void NotifyChange(string propertyName)
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
