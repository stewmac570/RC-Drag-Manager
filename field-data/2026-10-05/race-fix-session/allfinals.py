import sqlite3, json, sys, shutil
db = sys.argv[1]
shutil.copy(db, db.replace('race_data.db', 'Backups/race_data_BEFORE-ALL-FINALS_2026-10-03.db'))
c = sqlite3.connect(db)
j = json.loads(c.execute('select EventData from MultiClassEvents where Id=17').fetchone()[0])
s = j['classSessions'][0]; r = s['resume']
assert s['raceType'] == 'Finals' and r['currentPhase'] == 'Finals'
assert not any(m['winnerId'] for m in r['mainMatches']), 'finals races already run'
ranked = [x['driverId'] for x in sorted(s['resultsArchive']['roundRobinStandings'], key=lambda x: x['rank'])]
ranked += [0] if len(ranked) % 2 else []
r['mainMatches'] = [{'matchId': i+1, 'driver1Id': ranked[2*i], 'driver2Id': ranked[2*i+1], 'roundLabel': 'R1',
                     'fromMatch1': None, 'fromMatch2': None, 'winnerId': None, 'loserId': None} for i in range(len(ranked)//2)]
s['savedRevealedRounds'] = ['R1']
r['activeRound'] = None
c.execute('update MultiClassEvents set EventData=? where Id=17', (json.dumps(j, ensure_ascii=False),))
c.commit()
print('all-finals patched: cars', len([d for d in ranked if d]))
