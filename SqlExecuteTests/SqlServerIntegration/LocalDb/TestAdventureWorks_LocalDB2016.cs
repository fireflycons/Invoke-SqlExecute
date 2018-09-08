namespace SqlExecuteTests.SqlServerIntegration.LocalDb
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestAdventureWorks_LocalDB2016 : TestAdventureWorksBase<LocalDb2016InstanceInfo>
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void BuildAdventureWorksDatabase_LocalDB2016()
        {
            this.BuildAdventureWorksOltpDatabase(this.TestContext);
        }
    }
}