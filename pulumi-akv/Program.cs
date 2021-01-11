using System;
using Pulumi;
using Pulumi.AzureNextGen.Authorization.Latest;
using Pulumi.AzureNextGen.KeyVault.Latest;
using Pulumi.AzureNextGen.KeyVault.Latest.Inputs;
using Pulumi.AzureNextGen.Resources.Latest;
using Pulumi.Random;
using Deployment = Pulumi.Deployment;

await Deployment.RunAsync<AkvDemoStack>();

class AkvDemoStack : Stack
{
    public AkvDemoStack()
    {
        var rg = new ResourceGroup("rg", new ResourceGroupArgs()
        {
            ResourceGroupName = "test-akv",
            Location = "westeurope"
        });
        
        var azureId = Output
            .Create(GetClientConfig.InvokeAsync())
            .Apply(r => (r.TenantId, r.ObjectId));

        var vaultName = new RandomString("vaultname", new(){
            Length = 12,
            Special = false,
            Upper = false,
            Number = false
        });

        var vault = new Vault("vault", new ()
        {
            Location = rg.Location,
            ResourceGroupName = rg.Name,
            VaultName = vaultName.Result,
            Properties = new VaultPropertiesArgs
            {
                Sku = new SkuArgs()
                {
                    Name = SkuName.Standard,
                    Family = SkuFamily.A
                },
                SoftDeleteRetentionInDays = 14,
                TenantId = azureId.Apply(a => a.TenantId),
                AccessPolicies =
                {
                    new AccessPolicyEntryArgs()
                    {
                        ObjectId = azureId.Apply(a => a.ObjectId),
                        TenantId = azureId.Apply(a => a.TenantId),
                        Permissions = new PermissionsArgs()
                        {
                            Keys = {KeyPermissions.All}
                        }
                    }
                }
            }
        });

        new Key("k", new()
        {
            ResourceGroupName = rg.Name,
            VaultName = vault.Name,
            KeyName = "key",
            Properties = new KeyPropertiesArgs()
            {
                KeySize = 2048,
                Kty = "RS",
                KeyOps =
                {
                    JsonWebKeyOperation.WrapKey,
                    JsonWebKeyOperation.UnwrapKey
                },
            }
        });
    }
}