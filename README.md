# SQL Falsifier

Small WPF tool that takes a SQL script (`SELECT`, `CREATE TABLE`, `CREATE VIEW`, ...) and
replaces every table / view / column / alias name with a pronounceable fake name, so a
query can be shared without leaking the business vocabulary.

```bash
dotnet run --project SqlFalsifier.csproj
```

## Guarantees

- **Stable mapping** — a given real word always maps to the same fake word, everywhere in
  the script and across runs (same seed).
- **Coherent composites** — identifiers are split into words, so `Client`, `IdClient`,
  `NomClient`, `CLIENT_ID_REF` and `[Nom du client]` all reuse the same fake for `Client`.
  The `Id` / `Fk` / `Pk` affixes are kept verbatim: `IdClient` → `IdFlefaihim`.
- **Casing and layout preserved** — `CLIENT` → `FLEFAIHIM`, `client` → `flefaihim`,
  `Client` → `Flefaihim`. Whitespace, indentation and quoting styles (`[x]`, `"x"`,
  `` `x` ``) come out unchanged.
- **Nothing else is touched** — keywords, data types, built-in functions, string literals,
  comments, numbers and `@@SYSTEM` variables are left alone.
- **Reversible** — `Restore` turns a falsified script back into the original using the
  current mapping.

## Options

| Option | Effect |
| --- | --- |
| Seed | Drives the fake-name generator; same seed ⇒ same output. |
| Split compound names | `DateFacture` is mapped as `Date` + `Facture` instead of one blob. |
| Rename `@variables` / `#temp` | `@@ROWCOUNT` and friends are never renamed. |
| Rename user function names | Off by default, so an unrecognised built-in is not mangled. |
| Keep these words | Affixes kept verbatim inside identifiers (`Id, Fk, Pk, Uk`). |
| Keep these names | Whole identifiers never renamed (`dbo, sys, INFORMATION_SCHEMA, guest`). |

The mapping grid is editable: type your own value in the *Fake word* column and hit
**Falsify** again to force it. Changing seed or any option rebuilds the mapping from
scratch; otherwise it is kept alive across runs so several scripts stay consistent with
each other.

## Layout

| Path | Role |
| --- | --- |
| `Core/SqlTokenizer.cs` | Lexer: separates identifiers from literals, comments and operators. |
| `Core/SqlKeywords.cs` | Reserved words, data types and built-in functions that must survive. |
| `Core/IdentifierSplitter.cs` | Word splitting (camelCase, `_`, affixes) and casing transfer. |
| `Core/FakeWordGenerator.cs` | Deterministic pronounceable word generator. |
| `Core/QueryFalsifier.cs` | Ties it together: `Falsify` / `Restore` + the name map. |
| `MainWindow.xaml(.cs)` | UI. |

## Known limits

It is a lexer, not a parser. An identifier that shadows an unknown built-in function is
renamed like any other name (hence the *Rename user function names* switch, off by
default), and dynamic SQL built inside string literals is left untouched by design.
