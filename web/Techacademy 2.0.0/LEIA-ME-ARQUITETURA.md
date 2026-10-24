# TechAcademy — versão local com HTML, CSS e JavaScript separados

## Organização do código

- `index.html`: estrutura da tela de acesso.
- `dashboard-professor.html`: estrutura fixa do painel do professor e templates HTML das telas/fragmentos do professor.
- `dashboard-aluno.html`: estrutura fixa do painel do aluno e templates HTML das telas/fragmentos do aluno.
- `css/styles.css`: regras visuais próprias, responsividade e estados dos componentes. As classes utilitárias existentes do Tailwind continuam sendo usadas para preservar o desenho atual.
- `js/app-professor.js`: estado, navegação, formulários, notas, aulas, quizzes, notificações e persistência do painel do professor.
- `js/app-aluno.js`: estado, navegação, visualização de aulas/PDF, respostas dos quizzes, XP, sequência de atividade e persistência do painel do aluno.
- `js/login.js`: validação e comportamento da tela de acesso.
- `js/mobile-menu.js`: abre/fecha o menu lateral em telas pequenas sem gerar marcação HTML.

## Por que existem elementos `<template>` nos HTMLs?

As estruturas de cards, formulários e tabelas estão declaradas nos próprios HTMLs, dentro de elementos `<template>`. O JavaScript busca esses templates e injeta apenas os valores dinâmicos (nomes, títulos, datas, notas e opções). Assim, a marcação não fica escondida em longas strings dentro dos arquivos JS. Marcadores como `__TA_SLOT_0__` indicam locais preenchidos pelo JavaScript.

A interface principal é estática no HTML; elementos repetidos gerados a partir dos dados (por exemplo, uma linha por aluno ou um card por quiz) são clonados/preenchidos em tempo de execução, como é normal em aplicações web. Eventos são registrados no JavaScript, sem atributos `onclick` nos templates.

## Como executar

1. Extraia o ZIP inteiro para uma pasta.
2. Abra `index.html` no navegador para a simulação local.
3. Os dados de demonstração são guardados em `localStorage` do navegador; não há API nem banco de dados remoto nesta versão.
4. O projeto usa Tailwind CSS, Font Awesome e Google Fonts por CDN, portanto a aparência completa depende de conexão à internet.

## Limitações conhecidas da versão local

- `localStorage` pertence ao navegador e à origem onde o projeto é aberto. Se abrir os arquivos por caminhos/origens diferentes, os dados podem não ser compartilhados.
- PDFs são armazenados como dados locais e há limite prático de tamanho indicado pela própria interface.
- Os dados iniciais são de demonstração; não são autenticação real nem um sistema multiusuário seguro.

## Comentários no código

Os arquivos JS incluem comentários de função em português e comentários de seção sobre estado, persistência, templates e eventos. Os HTMLs contêm comentários antes das áreas principais e de cada template reutilizável; o CSS documenta os grupos de regras.
