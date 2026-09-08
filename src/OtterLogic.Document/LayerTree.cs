using Rhino;
using Rhino.DocObjects;

namespace OtterLogic.Document;

/// <summary>
/// Reading the document's layer table as a flat, ordered list.
/// <para>
/// Flat rather than nested, because every caller so far wants to walk it in
/// order — draw it as a list, resolve a selection against it, report on it — and
/// a nested tree makes all three of those a recursion for no gain. The nesting
/// survives as <see cref="LayerNode.Depth"/>, which is what a list needs anyway.
/// </para>
/// </summary>
public static class LayerTree
{
    /// <summary>
    /// Every layer in the document, depth first, each parent immediately
    /// followed by its own descendants.
    /// <para>
    /// Siblings come out in <c>SortIndex</c> order, which is the order the Rhino
    /// layer panel shows them. A list that disagrees with the panel sitting
    /// beside it is a list nobody trusts.
    /// </para>
    /// <para>
    /// No document gives an empty list rather than an exception. A front-end
    /// asks this on every repaint and every document event, and "nothing to
    /// show" is an ordinary state for it to be in — Rhino is perfectly capable
    /// of having no active document while Grasshopper is open.
    /// </para>
    /// </summary>
    public static IReadOnlyList<LayerNode> Read(RhinoDoc? doc)
    {
        var nodes = new List<LayerNode>();
        if (doc is null) return nodes;

        // Grouped by parent first, so the tree can be walked in one pass
        // afterwards without asking the table for children over and over.
        var children = new Dictionary<Guid, List<Layer>>();

        foreach (Layer layer in doc.Layers)
        {
            if (layer.IsDeleted) continue;

            if (!children.TryGetValue(layer.ParentLayerId, out List<Layer>? siblings))
                children[layer.ParentLayerId] = siblings = new List<Layer>();

            siblings.Add(layer);
        }

        foreach (List<Layer> siblings in children.Values)
            siblings.Sort((a, b) => a.SortIndex.CompareTo(b.SortIndex));

        Walk(Guid.Empty, 0);

        return nodes;

        void Walk(Guid parent, int depth)
        {
            if (!children.TryGetValue(parent, out List<Layer>? siblings)) return;

            foreach (Layer layer in siblings)
            {
                bool hasChildren = children.TryGetValue(layer.Id, out List<Layer>? kids)
                                   && kids.Count > 0;

                nodes.Add(new LayerNode(
                    layer.FullPath, layer.Name, depth, layer.Color.ToArgb(), hasChildren));

                Walk(layer.Id, depth + 1);
            }
        }
    }

    /// <summary>
    /// The nodes a folded parent is not hiding: everything from
    /// <paramref name="nodes"/> except the descendants of the parents named in
    /// <paramref name="collapsed"/>.
    /// <para>
    /// Folding is a display state rather than a document one, which is why it
    /// arrives as an argument instead of living here. Two views of the same
    /// document can fold different branches, and neither is more right.
    /// </para>
    /// <para>
    /// A path in <paramref name="collapsed"/> that names no parent — a layer
    /// since deleted, or one that has lost its children — hides nothing rather
    /// than being an error, so a stale fold state is harmless.
    /// </para>
    /// </summary>
    public static IReadOnlyList<LayerNode> Visible(
        IReadOnlyList<LayerNode> nodes, IReadOnlySet<string> collapsed)
    {
        var visible = new List<LayerNode>(nodes.Count);

        // The nodes are depth first, so everything under a folded parent is the
        // run immediately after it of nodes deeper than it is. One pass, no
        // recursion and no lookups.
        int foldedAt = int.MaxValue;

        foreach (LayerNode node in nodes)
        {
            if (node.Depth > foldedAt) continue;

            foldedAt = node.HasChildren && collapsed.Contains(node.Path)
                ? node.Depth
                : int.MaxValue;

            visible.Add(node);
        }

        return visible;
    }
}
