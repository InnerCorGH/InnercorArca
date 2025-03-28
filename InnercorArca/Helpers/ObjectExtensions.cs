using System;

public static class ObjectExtensions
{
    /// <summary>
    /// Copia todas las propiedades del objeto base hacia un objeto derivado.
    /// </summary>
    public static TDerived CopyToDerived<TBase, TDerived>(this TBase source)
        where TDerived : TBase, new()
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        TDerived dest = new TDerived();

        var properties = typeof(TBase).GetProperties();
        foreach (var prop in properties)
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(source);
                prop.SetValue(dest, value);
            }
        }

        return dest;
    }
}

