using System;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers.FlightTools
{
    internal abstract class ItemController<T, TVisualElement> where TVisualElement : VisualElement, new()
    {
        public T? Model { get; set; }
        
        public Action? DestroyButtonClicked;

        public TVisualElement Item { get; } = new();

        public void Refresh()
        {
            SyncTo(Model);
        }
        
        public abstract void SyncTo(T? model);
    }
}