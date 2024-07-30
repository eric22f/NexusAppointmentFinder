SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NexusUserLocations](
	[UserId] [int] NOT NULL,
	[LocationId] [int] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NexusUserLocations] ADD  CONSTRAINT [PK_NexusUserLocations] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LocationId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NexusUserLocations]  WITH CHECK ADD  CONSTRAINT [FK_NexusUserLocations_NexusLocations] FOREIGN KEY([LocationId])
REFERENCES [dbo].[NexusLocations] ([LocationId])
GO
ALTER TABLE [dbo].[NexusUserLocations] CHECK CONSTRAINT [FK_NexusUserLocations_NexusLocations]
GO
ALTER TABLE [dbo].[NexusUserLocations]  WITH CHECK ADD  CONSTRAINT [FK_NexusUserLocations_NexusUsers] FOREIGN KEY([UserId])
REFERENCES [dbo].[NexusUsers] ([UserId])
GO
ALTER TABLE [dbo].[NexusUserLocations] CHECK CONSTRAINT [FK_NexusUserLocations_NexusUsers]
GO
