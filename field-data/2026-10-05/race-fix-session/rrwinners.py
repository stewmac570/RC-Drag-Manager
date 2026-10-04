import sqlite3, json, sys, shutil
db = sys.argv[1]
shutil.copy(db, db.replace('race_data.db', 'Backups/race_data_BEFORE-RR-WINNERS_2026-10-03.db'))
orig = json.load(open(sys.argv[0].replace('rrwinners.py', 'ev17.json')))['classSessions'][0]['resume']['rrSnapshotMatches']
assert sum(1 for m in orig if m['winnerId']) == 27
c = sqlite3.connect(db)
j = json.loads(c.execute('select EventData from MultiClassEvents where Id=17').fetchone()[0])
s = j['classSessions'][0]
s['resume']['rrSnapshotMatches'] = orig
c.execute('update MultiClassEvents set EventData=? where Id=17', (json.dumps(j, ensure_ascii=False),))
c.commit()
print('round robin results restored: 27')
