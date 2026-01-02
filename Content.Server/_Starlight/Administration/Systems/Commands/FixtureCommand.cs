using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Toolshed;

namespace Content.Server._Starlight.Administration.Systems.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class FixtureCommand : ToolshedCommand
{
    private FixtureSystem? _fixture;

    [CommandImplementation("createaabb")]
    public EntityUid CreateFixture([PipedArgument] EntityUid uid, string id, IPhysShape shape, float density, bool hard,
        int collisionLayer, int collisionMask, float friction, float restitution)
    {
        _fixture ??= GetSys<FixtureSystem>();
        var phys = EnsureComp<PhysicsComponent>(uid);
        var fixtures = EnsureComp<FixturesComponent>(uid);
        var xform = EnsureComp<TransformComponent>(uid);
        _fixture.TryCreateFixture(uid, shape, id, density, hard, collisionLayer, collisionMask, friction, restitution,
            true, fixtures, phys, xform);
        return uid;
    }

    [CommandImplementation("create")]
    public IEnumerable<EntityUid> CreateFixture([PipedArgument] IEnumerable<EntityUid> uid, string id, IPhysShape shape,
        float density, bool hard, int collisionLayer, int collisionMask, float friction, float restitution) =>
        uid.Select(x =>
            CreateFixture(x, id, shape, density, hard, collisionLayer, collisionMask, friction, restitution));

    [CommandImplementation("delete")]
    public EntityUid DeleteFixture([PipedArgument] EntityUid uid, string id)
    {
        _fixture ??= GetSys<FixtureSystem>();
        _fixture.DestroyFixture(uid, id);
        return uid;
    }

    [CommandImplementation("delete")]
    public IEnumerable<EntityUid> DeleteFixture([PipedArgument] IEnumerable<EntityUid> uid, string id) =>
        uid.Select(x => DeleteFixture(x, id));
}