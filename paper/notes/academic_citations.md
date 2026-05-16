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

## 2. ASP.NET Core Identity

### Citarea documentației oficiale
```bibtex
@manual{aspnet_identity_docs,
  title        = {Introduction to Identity on ASP.NET Core},
  author       = {{Microsoft Corporation}},
  organization = {Microsoft Learn},
  year         = {2024},
  url          = {https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity},
  note         = {Accesat: Mai 2026}
}
```

### Exemplu de utilizare în text:
> „Pentru gestionarea utilizatorilor și asigurarea securității datelor (hash-uirea parolelor, generarea de token-uri), sistemul backend integrează framework-ul \textit{ASP.NET Core Identity}. Această soluție a fost aleasă deoarece oferă nativ o arhitectură robustă pentru autentificare și stocare securizată, compatibilă direct cu Entity Framework Core \cite{aspnet_identity_docs}.”

---

## 3. Echilibrarea Bibliografiei (Formula Câștigătoare)

Este important să nu avem doar link-uri către documentații. O proporție recomandată pentru 20 de citări:

1.  **Cărți fundamentale / Teorie (5-7 surse):** Validează arhitectura (ex: Design Patterns, Game Programming Patterns, C# in a Nutshell).
2.  **Documentații Oficiale (aprox. 10 surse):** Validează implementarea (ASP.NET Identity, Godot, EF Core, SQLite).
3.  **Articole științifice / de specialitate (2-3 surse):** Arată cercetare specifică (matchmaking, algoritmi, AI).
