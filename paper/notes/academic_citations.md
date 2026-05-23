# Notițe Citări Academice și Bibliografie

Aceste notițe conțin sfaturi pentru redactarea bibliografiei și citarea corectă a tehnologiilor folosite în lucrarea de licență.

## 1. Godot Engine

### Citarea Oficială a Motorului (Software Citation)
```bibtex
@software{godot_engine,
  author       = {Juan Linietsky and Ariel Manzur and {The Godot Community}},
  title        = {Godot Engine},
  version      = {4.x},
  publisher    = {Godot Foundation},
  year         = {2014},
  url          = {https://godotengine.org}
}
```

### Citarea unui Paper Științific (Academic Validation)
```bibtex
@article{godot_mdpi_paper,
  author         = {Eduardo Soares and others},
  title          = {Deep Reinforcement Learning with Godot Game Engine},
  journal        = {Electronics},
  volume         = {13},
  number         = {5},
  pages          = {985},
  year           = {2024},
  publisher      = {MDPI},
  doi            = {10.3390/electronics13050985}
}
```

---

@article{10.1145/320493.320489,
author = {Bernstein, Philip A.},
title = {Synthesizing third normal form relations from functional dependencies},
year = {1976},
issue_date = {Dec. 1976},
publisher = {Association for Computing Machinery},
address = {New York, NY, USA},
volume = {1},
number = {4},
issn = {0362-5915},
url = {https://doi.org/10.1145/320493.320489},
doi = {10.1145/320493.320489},
abstract = {It has been proposed that the description of a relational database can be formulated as a set of functional relationships among database attributes. These functional relationships can then be used to synthesize algorithmically a relational scheme. It is the purpose of this paper to present an effective procedure for performing such a synthesis. The schema that results from this procedure is proved to be in Codd's third normal form and to contain the fewest possible number of relations. Problems with earlier attempts to construct such a procedure are also discussed.},
journal = {ACM Trans. Database Syst.},
month = dec,
pages = {277–298},
numpages = {22},
keywords = {third normal form, semantics of data, relational model, functional dependency, database schema}
}

### Exemplu de utilizare în text:
> „Pentru gestionarea utilizatorilor și asigurarea securității datelor (hash-uirea parolelor, generarea de token-uri), sistemul backend integrează framework-ul \textit{ASP.NET Core Identity}. Această soluție a fost aleasă deoarece oferă nativ o arhitectură robustă pentru autentificare și stocare securizată, compatibilă direct cu Entity Framework Core \cite{aspnet_identity_docs}.”

---

## 3. Echilibrarea Bibliografiei (Formula Câștigătoare)

Este important să nu avem doar link-uri către documentații. O proporție recomandată pentru 20 de citări:

1.  **Cărți fundamentale / Teorie (5-7 surse):** Validează arhitectura (ex: Design Patterns, Game Programming Patterns, C# in a Nutshell).
2.  **Documentații Oficiale (aprox. 10 surse):** Validează implementarea (ASP.NET Identity, Godot, EF Core, SQLite).
3.  **Articole științifice / de specialitate (2-3 surse):** Arată cercetare specifică (matchmaking, algoritmi, AI).
