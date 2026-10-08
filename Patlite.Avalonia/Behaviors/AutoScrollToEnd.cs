using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Patlite.Avalonia.Behaviors;

public sealed class AutoScrollToEnd : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<AutoScrollToEnd, ListBox, bool>("IsEnabled");

    private static readonly AttachedProperty<Subscription?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<AutoScrollToEnd, ListBox, Subscription?>("Subscription");

    static AutoScrollToEnd()
    {
        IsEnabledProperty.Changed.AddClassHandler<ListBox>((list, _) =>
        {
            list.GetValue(SubscriptionProperty)?.Dispose();
            list.SetValue(SubscriptionProperty, GetIsEnabled(list) ? new Subscription(list) : null);
        });
    }

    public static bool GetIsEnabled(ListBox list) => list.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ListBox list, bool value) => list.SetValue(IsEnabledProperty, value);

    private sealed class Subscription : IDisposable
    {
        private readonly ListBox _list;
        private bool _subscribed;
        private bool _pending;

        public Subscription(ListBox list)
        {
            _list = list;
            _list.AttachedToVisualTree += OnAttached;
            _list.DetachedFromVisualTree += OnDetached;
            if (_list.IsAttachedToVisualTree())
            {
                Subscribe();
            }
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Subscribe();

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            _list.Items.CollectionChanged += OnItemsChanged;
            QueueScroll();
        }

        private void Unsubscribe()
        {
            _list.Items.CollectionChanged -= OnItemsChanged;
            _subscribed = false;
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => QueueScroll();

        private void QueueScroll()
        {
            if (_pending)
            {
                return;
            }

            _pending = true;
            // Coalesce log updates and wait for the bound items to reach the view.
            Dispatcher.UIThread.Post(() =>
            {
                _pending = false;
                if (_subscribed && _list.Items.Count > 0 && _list.Items[^1] is { } item)
                {
                    _list.ScrollIntoView(item);
                }
            }, DispatcherPriority.Loaded);
        }

        public void Dispose()
        {
            Unsubscribe();
            _list.AttachedToVisualTree -= OnAttached;
            _list.DetachedFromVisualTree -= OnDetached;
        }
    }
}
