using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Toolshed;

namespace Content.Server.Storage;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class StorageCommand : ToolshedCommand
{
    private SharedStorageSystem? _storage;
    private SharedContainerSystem? _container;
    private SharedUserInterfaceSystem? _ui; // starlight

    [CommandImplementation("insert")]
    public IEnumerable<EntityUid> StorageInsert([PipedArgument] IEnumerable<EntityUid> entsToInsert,
        EntityUid targetEnt) => entsToInsert.Where(x => StorageInsert(x, targetEnt) != null);

    public EntityUid? StorageInsert(EntityUid entToInsert, EntityUid targetEnt)
    {
        _storage ??= GetSys<SharedStorageSystem>();

        if (!EntityManager.TryGetComponent<StorageComponent>(targetEnt, out var storage))
            return null;

        return _storage.Insert(targetEnt, entToInsert, out var stackedEntity, null, storage, false)
            ? entToInsert
            : null;
    }


    [CommandImplementation("fasttake")]
    public IEnumerable<EntityUid> StorageFastTake([PipedArgument] IEnumerable<EntityUid> storageEnts) =>
        storageEnts.Select(StorageFastTake).OfType<EntityUid>();

    public EntityUid? StorageFastTake(EntityUid storageEnt)
    {
        _storage ??= GetSys<SharedStorageSystem>();
        _container ??= GetSys<SharedContainerSystem>();


        if (!EntityManager.TryGetComponent<StorageComponent>(storageEnt, out var storage))
            return null;

        var removing = storage.Container.ContainedEntities[^1];
        if (_container.RemoveEntity(storageEnt, removing))
            return removing;

        return null;
    }

    //Starlight begin
    [CommandImplementation("reshape")]
    public EntityUid StorageResize([PipedArgument] EntityUid uid, BoxList shapes)
    {
        _storage ??= GetSys<SharedStorageSystem>();
        _container ??= GetSys<SharedContainerSystem>();
        _ui ??= GetSys<SharedUserInterfaceSystem>();
        if (!TryComp<StorageComponent>(uid, out var storage)) return uid;
        _container.EmptyContainer(storage.Container);
        storage.Grid = shapes.Boxes;
        _storage.UpdateOccupied((uid, storage)); // <- this is black voodoo magic and i hate it
        _ui.CloseUi(uid, StorageComponent.StorageUiKey.Key);
        return uid;
    }
    //Starlight end
}

//Starlight begin
public readonly record struct BoxList(List<Box2i> Boxes)
{
    public override string ToString()
    {
        var str = Boxes.Aggregate("BoxList[",
            (current, box) => current + $"{{{box.Top},{box.Left},{box.Bottom},{box.Right}}},");
        if (str.EndsWith(',')) str = str.Remove(str.Length - 1);
        str += ']';
        return str;
    }
}
//Starlight end
