# Visão geral da arquitetura

## Objetivo

Organizar o TechAcademy em módulos com responsabilidades distintas, preservando o site existente e desenvolvendo os componentes restantes de forma integrada.

## Componentes

1. **Web:** repositório existente [kevinzin030/site-techacademy](https://github.com/kevinzin030/site-techacademy). Será analisado e adaptado sem apagar nem substituir o código atual.
2. **API:** Node.js e Express, responsável por expor endpoints, validar entradas, aplicar autenticação/autorização e mediar o acesso aos dados.
3. **Desktop:** aplicação C# com Windows Forms para as funções administrativas previstas nos requisitos.
4. **Mobile:** aplicativo Android para consulta de informações acadêmicas, incluindo notas e frequência.
5. **Banco de dados:** persistência compartilhada por meio da API, com modelo e scripts documentados em `database/`.

## Fluxo lógico previsto

```text
Web existente ─────┐
                   ├──> API Node.js / Express ───> Banco de dados
Desktop WinForms ──┤
                   │
Mobile Android ────┘
```

O diagrama é uma proposta inicial. A implementação deverá confirmar a tecnologia de banco, autenticação, endpoints e permissões a partir do código atual e dos requisitos oficiais.

## Regras de integração

- Clientes acessam os dados por endpoints documentados da API.
- Credenciais e segredos ficam em variáveis de ambiente, nunca no repositório.
- A API deve validar dados e aplicar autorização por perfil.
- O lançamento de notas permanece no fluxo web previsto.
- A frequência segue o fluxo web definido para o projeto; o mobile é destinado à consulta.
- O aplicativo mobile não deve ser considerado fonte de alteração de notas ou frequência sem aprovação explícita dos requisitos.

## Repositório web existente

O código original permanece em: https://github.com/kevinzin030/site-techacademy

Este repositório central não substitui nem altera aquele projeto automaticamente. A integração do conteúdo web deve ser feita depois de revisar sua estrutura, dependências, histórico e eventuais segredos antes de copiá-lo ou adicioná-lo como submódulo.
