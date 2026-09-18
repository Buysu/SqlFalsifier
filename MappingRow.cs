using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SqlFalsifier;

public sealed class MappingRow : INotifyPropertyChanged
{
    private string _fake = string.Empty;
    private int _count;

    public required string Real { get; init; }

    public string Fake
    {
        get => _fake;
        set => Set(ref _fake, value);
    }

    public int Count
    {
        get => _count;
        set => Set(ref _count, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
