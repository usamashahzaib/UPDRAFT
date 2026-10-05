using NUnit.Framework;
using Hotdrop.Core;

namespace Hotdrop.Tests
{
    /// <summary>Retention UI must not leak into ordinary builds without an explicit staging asset.</summary>
    public class ContractsMasteryFeatureIsolationTests
    {
        [Test]
        public void ContractsAndMastery_AreDisabledWithoutAnExplicitFeatureAsset()
        {
            Assert.IsFalse(ContractsMasteryFeature.Enabled);
        }
    }
}
