// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

// A GHOSTS-like app on the Chronicle: it subscribes as the simulated office staff would, sees only what they may see,
// and publishes back what its NPCs do. Everything Chronicle-shaped is the library's; NpcHandler is the app's own.

using Chronicle.Client;
using Chronicle.Client.Hosting;
using GhostsSubscriber;

var builder = Host.CreateApplicationBuilder(args);

// Where the Chronicle API is, and who we are (Chronicle:Url, Chronicle:App).
builder.Services.AddChronicleClient(builder.Configuration);

// Resume where we left off after a restart; without this, a restart replays the history.
builder.Services.AddSingleton<ICursorStore>(new FileCursorStore(Path.Combine(AppContext.BaseDirectory, "cursors")));

// One subscription, from Chronicle:Subscribers:office-staff. A real GHOSTS would add one per persona or pool.
builder.Services.AddChronicleSubscriber<NpcHandler>("office-staff", builder.Configuration.GetSection("Chronicle:Subscribers:office-staff"));

builder.Build().Run();
