# OtterLogic — Document

Getting at what is already in the Rhino document: layers, objects, references.

A **domain** in the OtterLogic layering — it sits above
[Core](https://github.com/Otter-Logic/Core) and below
[Rhino3D](https://github.com/Otter-Logic/Rhino3D), and knows nothing about
either Grasshopper or the Rhino UI. Nothing here builds geometry; it points at
geometry the other domains built.

## Where the line falls

Every other domain reads RhinoCommon for its *geometry* types — `Curve`,
`Line`, `Point3d`. This one reads it for the *document tables* as well, because
that is the whole subject: a library about the Rhino document cannot avoid
`RhinoDoc`.

What stays out is everything above it. No `GH_Component`, no canvas drawing, no
commands or panels, and no document *events* — a front-end decides when to ask,
and this only answers. So the split for the Layer Picker is:

| | |
|---|---|
| here | reading the layer table, ordering it, folding branches away |
| Rhino3D | the component, its ports, the tick boxes drawn on the canvas, and the `RhinoDoc` event wiring that decides when to re-read |

## Layers

`LayerTree.Read(doc)` returns the whole table flattened depth first — each
parent immediately followed by its own descendants, siblings in the order the
Rhino layer panel shows them. Each entry is a `LayerNode`:

```csharp
foreach (LayerNode node in LayerTree.Read(RhinoDoc.ActiveDoc))
    Console.WriteLine($"{new string(' ', node.Depth * 2)}{node.Name}  →  {node.Path}");
```

```
OtterFlatTruss1  →  OtterFlatTruss1
  Top Chord      →  OtterFlatTruss1::Top Chord
  Diagonal       →  OtterFlatTruss1::Diagonal
Default          →  Default
```

`Path` is the handle to hold onto — `::` between levels, which is Rhino's own
notation and what its object queries read back, so a path from here goes
straight into a layer filter with nothing in between. It is also the only handle
that survives a saved file: indices and ids are per-document, so a definition
reopened against a rebuilt model would resolve nothing by either.

`LayerTree.Visible(nodes, collapsed)` drops the descendants of any parent named
in `collapsed`, for a front-end that lets branches be folded away. Folding is a
display state, not a document one, which is why it is an argument rather than
something stored here — two views of one document can fold different branches
and neither is more right.

`LayerNode.IsUnder(path)` answers the other half of that: whether a node sits
somewhere beneath a given ancestor. It matches on the separator rather than the
bare name, so `Truss1::Diagonal` is under `Truss1` and `Truss10` is not.

Colours come out as packed ARGB rather than a `System.Drawing.Color`, so nothing
above has to agree with a domain about a drawing type. `Color.FromArgb` turns
one back.

## Build

```bash
dotnet build OtterLogic.slnx
```

Clone [Core](https://github.com/Otter-Logic/Core) as a sibling of this repo and
the project reference resolves against your working copy; without one it falls
through to the published package.

## License

[Apache 2.0](LICENSE).
