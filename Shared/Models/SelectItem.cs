using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class SelectItem<T>
    {
        public T Value { get; }
        public string Text { get; }

        [JsonConstructor]
        public SelectItem(T value, string text)
        {
            Value = value;
            Text = text;
        }
    }
}
