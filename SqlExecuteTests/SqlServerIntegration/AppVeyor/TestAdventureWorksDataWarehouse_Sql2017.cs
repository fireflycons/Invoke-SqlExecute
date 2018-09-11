using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable InconsistentNaming
namespace SqlExecuteTests.SqlServerIntegration.AppVeyor
{
    [TestClass]
    public class TestAdventureWorks_Sql2017 : TestAdventureWorksBase<Sql2017InstanceInfo>
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Builds the adventure works data warehouse 2017.
        /// </summary>
        [TestMethod, Ignore]
        public void BuildAdventureWorksDataWarehouse_2017()
        {
            this.BuildAdventureWorksDataWarehouse(this.TestContext);
        }
    }
}
