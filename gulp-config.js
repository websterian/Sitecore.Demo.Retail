module.exports = function() {
    var instanceRoot = "C:\\inetpub\\sc82u1";
  var config = {
    websiteRoot: instanceRoot + "\\Website",
    sitecoreLibraries: instanceRoot + "\\Website\\bin",
    licensePath: instanceRoot + "\\Data\\license.xml",
    solutionName: "Sitecore.Demo.Retail",
    buildConfiguration: "Demo-Retail",
    buildPlatform: "Any CPU",
    publishPlatform: "AnyCpu",
    runCleanBuilds: false,
    commerceServerSiteName: "storefront",
    commerceEngineRoot: instanceRoot + "\\CommerceEngine",
    commerceDatabasePath: ".\\src\\Project\\Retail\\Database"
  };
  return config;
};