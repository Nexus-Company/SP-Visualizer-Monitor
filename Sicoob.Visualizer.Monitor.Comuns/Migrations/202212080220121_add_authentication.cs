namespace Sicoob.Visualizer.Monitor.Comuns.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class add_authentication : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GraphAuthentications",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    AccessToken = c.String(nullable: false, maxLength: 2500),
                    TokenType = c.String(maxLength: 100),
                    RefreshToken = c.String(maxLength: 2500),
                    ExpiresIn = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.GraphAuthentications");
        }
    }
}
