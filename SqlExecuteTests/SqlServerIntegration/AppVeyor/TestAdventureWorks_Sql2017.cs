using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        [TestMethod]
        public void BuildAdventureWorksDatabase_Sql2017()
        {
            this.BuildAdventureWorksDatabase(this.TestContext);
        }
    }
}
