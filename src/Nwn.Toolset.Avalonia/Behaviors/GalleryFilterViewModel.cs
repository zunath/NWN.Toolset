using CommunityToolkit.Mvvm.ComponentModel;

namespace Nwn.Toolset.Avalonia.Behaviors
{
    public sealed class GalleryFilterViewModel : ObservableObject
    {
        private GalleryFilterOption _selectedOption;
        private readonly Action _selectionChanged;

        public string GroupKey { get; }
        public string Label { get; }
        public IReadOnlyList<GalleryFilterOption> Options { get; }

        public GalleryFilterOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (!SetProperty(ref _selectedOption, value))
                    return;

                _selectionChanged();
            }
        }

        public GalleryFilterViewModel(
            string groupKey,
            string label,
            IReadOnlyList<GalleryFilterOption> options,
            Action selectionChanged)
        {
            GroupKey = groupKey;
            Label = label;
            Options = options;
            _selectionChanged = selectionChanged;
            _selectedOption = options.First();
        }
    }
}
