using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable InconsistentNaming
namespace SqlExecuteTests.SqlServerIntegration.AppVeyor
{
    [TestClass]
    public class TestAdventureWorksDataWarehouse_Sql2016 : TestAdventureWorksBase<Sql2016InstanceInfo>
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
        /// Builds the adventure works data warehouse SQL2016.
        /// </summary>
        [TestMethod]
        public void BuildAdventureWorksDataWarehouse_Sql2016()
        {
            this.BuildAdventureWorksDataWarehouse(this.TestContext);
        }
    }
}
