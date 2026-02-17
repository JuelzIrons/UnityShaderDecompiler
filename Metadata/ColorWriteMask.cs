namespace USCSandbox.Metadata;
[Flags]
public enum ColorWriteMask
{
    None = 0,
    Alpha = 1,
    Blue = 2,
    Green = 4,
    Red = 8,
    All = Red | Green | Blue | Alpha
}