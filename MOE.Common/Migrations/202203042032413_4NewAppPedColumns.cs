namespace MOE.Common.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _4NewAppPedColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Signals", "Pedsare1to1", c => c.Boolean(nullable: false));
            AddColumn("dbo.Approaches", "PedestrianPhaseNumber", c => c.Int());
            AddColumn("dbo.Approaches", "IsPedestrianPhaseOverlap", c => c.Boolean(nullable: false));
            AddColumn("dbo.Approaches", "PedestrianDetectors", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Approaches", "PedestrianDetectors");
            DropColumn("dbo.Approaches", "IsPedestrianPhaseOverlap");
            DropColumn("dbo.Approaches", "PedestrianPhaseNumber");
            DropColumn("dbo.Signals", "Pedsare1to1");
        }
    }
}
