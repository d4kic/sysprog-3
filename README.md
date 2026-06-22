# sysprog-3
Koristeći Rx.NET i NY Times Most Popular API, prikupiti članke za određeni vremenski period. Rx vrši osnovno mapiranje dobijenih članaka i emituje ih kao poruke aktorima. Aktori čuvaju podatke o člancima kao interno stanje i implementiraju Topic Modeling koristeći SentimentAnalysis.NET ili ML.NET biblioteke, uzimajući u obzir naslov i sadržaj članka. Web server prima zahteve sa parametrom vremenskog perioda i prevodi ih u poruke aktorima. Prikazati identifikovane teme i dobijene rezultate.

Dokumentacija dostupna na linku: https://developer.nytimes.com/
