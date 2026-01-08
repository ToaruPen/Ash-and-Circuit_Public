using AshNCircuit.Core.Map;
using NUnit.Framework;

namespace AshNCircuit.Tests.Core
{
    public sealed class PropDefinitionJsonCoverageTests
    {
        [Test]
        public void ChestPropDefinition_IsPresent_AndPropInstanceUsesSameValues()
        {
            var definition = PropDefinition.Chest;

            Assert.That(definition.Id, Is.EqualTo(PropDefinition.ChestPropId));
            Assert.That(definition.DisplayName, Is.Not.Null.And.Not.Empty);
            Assert.That(definition.SpriteId, Is.Not.Null.And.Not.Empty);

            var prop = new PropInstance(PropDefinition.ChestPropId);
            Assert.That(prop.DisplayName, Is.EqualTo(definition.DisplayName));
            Assert.That(prop.SpriteId, Is.EqualTo(definition.SpriteId));
            Assert.That(prop.BlocksMovement, Is.EqualTo(definition.BlocksMovement));
            Assert.That(prop.BlocksLOS, Is.EqualTo(definition.BlocksLOS));
            Assert.That(prop.BlocksProjectiles, Is.EqualTo(definition.BlocksProjectiles));
        }
    }
}
