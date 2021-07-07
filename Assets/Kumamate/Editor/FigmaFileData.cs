namespace Kumamate
{
    public class FileNameConstructor
    {
        public static string ConstructFileName(string name, string id)
        {
            var source = name + "=" + id;
            // C#のfile create APIは/をエスケープする手段がないので/を-にする。
            // macのfile create系は:を自動的に/に変換するので、事前に-にする。
            return source.Replace("/", "-").Replace(":", "-");
        }
    }
}