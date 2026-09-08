namespace OtterLogic.Document;

/// <summary>
/// One layer, flattened out of the document's tree and ready to be listed.
/// <para>
/// A snapshot, not a live handle. Holding <c>Rhino.DocObjects.Layer</c> objects
/// would mean re-checking every one of them for deletion on every read, and a
/// front-end drawing this list repaints far more often than the layer table
/// changes.
/// </para>
/// <para>
/// The colour arrives as packed ARGB rather than a <c>System.Drawing.Color</c>,
/// so nothing above this has to agree with a domain about a drawing type. A
/// layer's colour is document data — it is what the Rhino layer panel shows in
/// its swatch column — and the front-end that wants to paint with it turns it
/// back with <c>Color.FromArgb</c>.
/// </para>
/// </summary>
/// <param name="Path">
/// Full path, <c>::</c> between the levels — <c>OtterFlatTruss1::Diagonal</c>.
/// This is the handle everything else uses, because it is the only one that
/// survives a round trip through a saved file: layer indices and ids are
/// per-document, so a definition reopened against a rebuilt model would resolve
/// nothing, or worse, resolve something else.
/// </param>
/// <param name="Name">The layer's own name, without its ancestors.</param>
/// <param name="Depth">How deep in the tree it sits. Root layers are zero.</param>
/// <param name="Argb">The layer's display colour, packed.</param>
/// <param name="HasChildren">Whether any layer sits under this one.</param>
public sealed record LayerNode(
    string Path, string Name, int Depth, int Argb, bool HasChildren)
{
    /// <summary>
    /// Rhino's own separator between the levels of a layer path.
    /// <para>
    /// Not an invention of this library. Rhino writes full paths this way and
    /// its own object queries read them back that way, which is what lets a path
    /// from here go straight into a layer filter with nothing in between.
    /// </para>
    /// </summary>
    public const string Separator = "::";

    /// <summary>
    /// Whether this layer sits somewhere beneath <paramref name="ancestor"/>.
    /// A layer is not under itself.
    /// <para>
    /// Case-insensitive, because Rhino treats layer names that way — and it
    /// matches on the separator rather than the bare name, so
    /// <c>Truss1::Diagonal</c> is under <c>Truss1</c> but <c>Truss10</c> is not.
    /// </para>
    /// </summary>
    public bool IsUnder(string ancestor) => IsUnder(Path, ancestor);

    /// <summary>
    /// The same test against a bare path, for a caller holding paths rather
    /// than nodes — a saved selection, say, which is a set of strings and
    /// outlives any one reading of the table.
    /// </summary>
    public static bool IsUnder(string path, string ancestor)
        => path.StartsWith(ancestor + Separator, StringComparison.OrdinalIgnoreCase);
}
