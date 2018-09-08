using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlExecuteTests.SqlServerIntegration.AppVeyor
{
    [TestClass]
    public class TestAdventureWorks_Sql2014 : TestAdventureWorksBase<Sql2014InstanceInfo>
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
        public void BuildAdventureWorksDatabase_Sql2014()
        {
            this.BuildAdventureWorksOltpDatabase(this.TestContext);
        }
    }
}
