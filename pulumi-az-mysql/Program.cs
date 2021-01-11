using System;
using Pulumi;
using Pulumi.AzureNextGen.Authorization.Latest;
using Pulumi.AzureNextGen.DBforMySQL.Latest;
using Pulumi.AzureNextGen.DBforMySQL.Latest.Inputs;
using Pulumi.AzureNextGen.Resources.Latest;
using Pulumi.Random;
using Deployment = Pulumi.Deployment;

await Deployment.RunAsync<AzMysqlDemo>();

class AzMysqlDemo : Stack
{
    public AzMysqlDemo()
    {
        var rg = new ResourceGroup("rg", new ResourceGroupArgs()
        {
            ResourceGroupName = "test-az-mysql",
            Location = "westeurope"
        });
        
        var azureId = Output
            .Create(GetClientConfig.InvokeAsync())
            .Apply(r => (r.TenantId, r.ObjectId));

        var mysqlname = new RandomString("mysqlname", new(){
            Length = 12,
            Special = false,
            Upper = false,
            Number = false
        });

        var pw = new RandomPassword("mysqlpw", new()
        {
            Length = 24
        }, new()
        {
            AdditionalSecretOutputs = { "Result "}
        });

        var mysql = new Server("mysql", new()
        {
            ResourceGroupName = rg.Name,
            Location = rg.Location,
            ServerName = mysqlname.Result,
            Properties = new ServerPropertiesForDefaultCreateArgs
            {
                AdministratorLogin = "mysql-admin",
                AdministratorLoginPassword = pw.Result,
                CreateMode = "Default",
                StorageProfile = new StorageProfileArgs
                {
                    BackupRetentionDays = 7,
                    GeoRedundantBackup = GeoRedundantBackup.Disabled,
                    StorageMB = 5120
                },
                Version = "8.0",
                MinimalTlsVersion = "1.2",
                SslEnforcement = SslEnforcementEnum.Enabled
            },
            Sku = new SkuArgs()
            {
                Name = "B_Gen5_1"
            }
        });

        var mysqlopts = new CustomResourceOptions()
        {
            Provider = new Pulumi.MySql.Provider("mysql", new()
            {
                Endpoint = mysql.FullyQualifiedDomainName.Apply(s => $"{s}:3306"),
                Username = mysql.AdministratorLogin,
                Password = pw.Result,
                Tls = "skip-verify"
            })
        };

        var db = new Pulumi.MySql.Database("db", new()
        {
            Name = "db"
        }, mysqlopts);

        var user = new Pulumi.MySql.User("user", new()
        {
            UserName = "user",
            PlaintextPassword = pw.Result
        }, mysqlopts);
    }
}