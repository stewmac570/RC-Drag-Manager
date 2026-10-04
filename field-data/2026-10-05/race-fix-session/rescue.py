import sqlite3, json, sys
db = sys.argv[1]
c = sqlite3.connect(db)
raw = c.execute('select EventData from MultiClassEvents where Id=17').fetchone()[0]
j = json.loads(raw)
s = j['classSessions'][0]
r = s['resume']
assert s['classType'] == 'DYO' and s['raceType'] == 'Losers Bracket'
assert not any(m['winnerId'] for m in r['losersMatches']), 'LB races already run'
s['raceType'] = 'Multi-Car Round Robin'
s['roundRobinVariant'] = 'QMDRA'
s['roundsToRun'] = len(r['rrSnapshotRoundOrder'])
s['buybackDrivers'] = []
s['savedRevealedRounds'] = list(r['rrSnapshotRoundOrder'])
r['currentPhase'] = 'Main'
r['mainMatches'] = r['rrSnapshotMatches']
r['losersMatches'] = []
r['inLosersPhase'] = False
r['finalsPending'] = False
r['activeRound'] = None
r['buybackChampionOverrideId'] = None
s['resultsArchive']['phases'] = [p for p in s['resultsArchive']['phases'] if p['phase'] != 'Losers Bracket']
c.execute('update MultiClassEvents set EventData=? where Id=17', (json.dumps(j, ensure_ascii=False),))
c.commit()
print('patched: rounds', s['roundsToRun'], 'main matches', len(r['mainMatches']))
