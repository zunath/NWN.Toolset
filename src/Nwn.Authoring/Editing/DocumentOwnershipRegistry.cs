using System.Runtime.CompilerServices;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing;

/// <summary>Tracks which open document sessions own each mutable node in a GFF graph.</summary>
internal static class DocumentOwnershipRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly ConditionalWeakTable<object, OwnerSet> Owners = new();

    internal static DocumentOwnershipRegistration Register(JsonGffDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var registration = new DocumentOwnershipRegistration();
        RegisterTree(registration, document);
        return registration;
    }

    internal static void RegisterReplacement(JsonGffDocument document, JsonGffStruct root)
    {
        foreach (var registration in GetOwners(document))
        {
            lock (SyncRoot)
            {
                if (registration.IsDisposed)
                    continue;

                registration.PruneDeadNodes();
            }

            RegisterTree(registration, root);
        }
    }

    internal static void RegisterAttached(object parent, object child)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        foreach (var registration in GetOwners(parent))
            RegisterTree(registration, child);
    }

    internal static DocumentOwnershipRegistration[] GetOwners(object node)
    {
        lock (SyncRoot)
            return Owners.TryGetValue(node, out var ownerSet)
                ? ownerSet.Registrations.ToArray()
                : Array.Empty<DocumentOwnershipRegistration>();
    }

    internal static void Release(DocumentOwnershipRegistration registration)
    {
        lock (SyncRoot)
        {
            if (registration.IsDisposed)
                return;

            foreach (var node in registration.GetLiveNodes())
            {
                if (!Owners.TryGetValue(node, out var ownerSet))
                    continue;
                ownerSet.Registrations.Remove(registration);
                if (ownerSet.Registrations.Count == 0)
                    Owners.Remove(node);
            }

            registration.MarkDisposed();
            registration.ClearNodes();
        }
    }

    private static void RegisterTree(DocumentOwnershipRegistration registration, object root)
    {
        var pending = new Stack<object>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        pending.Push(root);

        while (pending.TryPop(out var node))
        {
            if (!visited.Add(node))
                continue;

            lock (SyncRoot)
            {
                if (registration.IsDisposed)
                    return;

                if (!Owners.TryGetValue(node, out var ownerSet))
                {
                    ownerSet = new OwnerSet();
                    Owners.Add(node, ownerSet);
                }

                if (ownerSet.Registrations.Add(registration))
                    registration.Track(node);
            }

            switch (node)
            {
                case JsonGffDocument document:
                    pending.Push(document.Root);
                    break;
                case JsonGffStruct structure:
                    foreach (var (_, field) in structure.Entries)
                        pending.Push(field);
                    break;
                case JsonGffField field:
                    if (field.Struct is { } childStruct)
                        pending.Push(childStruct);
                    if (field.Elements is { } elements)
                    {
                        foreach (var element in elements)
                            pending.Push(element);
                    }
                    if (field.LocStringEntries is { } entries)
                    {
                        foreach (var entry in entries)
                            pending.Push(entry);
                    }
                    break;
            }
        }
    }

    private sealed class OwnerSet
    {
        public HashSet<DocumentOwnershipRegistration> Registrations { get; } = [];
    }
}
