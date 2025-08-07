const express = require("express"),
  asyncLib = require("async"),
  { Pool } = require("pg"),
  cookieParser = require("cookie-parser"),
  path = require("path"),
  app = express(),
  server = require("http").Server(app),
  io = require("socket.io")(server);

const port = process.env.PORT || 80;

// ─── ENVIRONMENT VARIABLE CHECK ───────────────────────────────
const requiredEnv = [
  "POSTGRES_USER",
  "POSTGRES_PASSWORD",
  "POSTGRES_HOST",
  "POSTGRES_PORT",
  "POSTGRES_DB",
];

const missingEnv = requiredEnv.filter((key) => !process.env[key]);
if (missingEnv.length > 0) {
  console.error(
    "❌ Missing required environment variables:",
    missingEnv.join(", ")
  );
  process.exit(1);
}

// ─── Build DB connection string from environment variables ─────
const finalDatabaseUrl = `postgresql://${process.env.POSTGRES_USER}:${process.env.POSTGRES_PASSWORD}@${process.env.POSTGRES_HOST}:${process.env.POSTGRES_PORT}/${process.env.POSTGRES_DB}`;

// ─── SOCKET.IO ────────────────────────────────────────────────
io.on("connection", function (socket) {
  socket.emit("message", { text: "Welcome!" });

  socket.on("subscribe", function (data) {
    socket.join(data.channel);
  });
});

// ─── MIDDLEWARE & ROUTES ─────────────────────────────────────
app.use(cookieParser());
app.use(express.urlencoded({ extended: true }));
app.use(express.static(path.join(__dirname, "views")));

app.get("/", function (req, res) {
  res.sendFile(path.resolve(__dirname, "views/index.html"));
});

app.get("/health", (req, res) => {
  res.status(200).send("OK");
});

// ─── SERVER LISTEN ────────────────────────────────────────────
server.listen(port, "0.0.0.0", function () {
  console.log("🚀 App running on port " + port);
});

// ─── POSTGRES CONNECTION ──────────────────────────────────────
const pool = new Pool({
  connectionString: finalDatabaseUrl,
});

let dbClient = null;

function connectToDb() {
  asyncLib.retry(
    { times: 1000, interval: 1000 },
    function (callback) {
      pool.connect(function (err, client, done) {
        if (err) {
          console.error("⏳ Waiting for DB...");
        }
        callback(err, client);
      });
    },
    function (err, client) {
      if (err) {
        return console.error("❌ Giving up connecting to DB");
      }
      console.log("✅ Connected to DB");
      dbClient = client;
      
      // Create votes table if it doesn't exist
      client.query(
        "CREATE TABLE IF NOT EXISTS votes (id VARCHAR(255) NOT NULL UNIQUE, vote VARCHAR(255) NOT NULL)",
        [],
        function (err) {
          if (err) {
            console.error("❌ Error creating table: " + err);
          } else {
            console.log("✅ Votes table ready");
          }
          getVotes(client);
        }
      );
    }
  );
}

function getVotes(client) {
  client.query(
    "SELECT vote, COUNT(id) AS count FROM votes GROUP BY vote",
    [],
    function (err, result) {
      if (err) {
        console.error("❌ Error performing query: " + err);
      } else {
        const votes = collectVotesFromResult(result);
        io.sockets.emit("scores", JSON.stringify(votes));
      }

      setTimeout(() => getVotes(client), 1000);
    }
  );
}

function collectVotesFromResult(result) {
  const votes = { a: 0, b: 0 };
  result.rows.forEach((row) => {
    votes[row.vote] = parseInt(row.count);
  });
  return votes;
}

// 🔁 Start DB connection retry loop
connectToDb();
