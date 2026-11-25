import { SafeAreaProvider, SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';
import React, { useState, useEffect } from 'react';

import {
  View,
  Image,
  Text,
  TextInput,
  TouchableOpacity,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
  Alert,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';

import { StatusBar } from 'expo-status-bar';

// Endereço da API Node.js acessível pelo emulador Android.
const ENDERECO_API = 'https://techacademy-api-2026-h0ebgpcgechch0c4.brazilsouth-01.azurewebsites.net/api';

// Perfis autorizados a utilizar o TechAcademy Mobile.
const PERFIL_PROFESSOR = 3;
const PERFIL_ALUNO = 4;

// Tempo máximo de espera por uma resposta da API.
const TEMPO_LIMITE_REQUISICAO = 15000;

// Paleta de cores utilizada em todas as telas.
const CORES = {
  azulPrincipal: '#2563EB',
  azulEscuro: '#1D4ED8',
  azulClaro: '#EFF6FF',
  azulBorda: '#DBEAFE',
  marinho: '#101D39',
  fundo: '#F4F7FC',
  branco: '#FFFFFF',
  textoPrincipal: '#17233D',
  textoSecundario: '#64748B',
  textoDiscreto: '#94A3B8',
  borda: '#E2E8F0',
  verde: '#059669',
  vermelho: '#DC2626',
  vermelhoClaro: '#FEF2F2',
};

// Executa requisições HTTP e interrompe conexões demoradas.
async function requisitarApi(caminho, configuracao = {}) {
  const controlador = new AbortController();

  const temporizador = setTimeout(() => {
    controlador.abort();
  }, TEMPO_LIMITE_REQUISICAO);

  try {
    const resposta = await fetch(
      `${ENDERECO_API}${caminho}`,
      {
        ...configuracao,
        signal: controlador.signal,
      }
    );

    const textoResposta = await resposta.text();

    let dadosResposta = {};

    if (textoResposta) {
      try {
        dadosResposta = JSON.parse(textoResposta);
      } catch {
        throw new Error(
          'A API retornou uma resposta inválida.'
        );
      }
    }

    return {
      resposta,
      dadosResposta,
    };
  } finally {
    clearTimeout(temporizador);
  }
}

// Retorna uma mensagem apropriada para erros de conexão.
function obterMensagemErroConexao(erro) {
  if (erro.name === 'AbortError') {
    return 'A conexão demorou demais. Tente novamente.';
  }

  if (
    erro.message ===
    'A API retornou uma resposta inválida.'
  ) {
    return erro.message;
  }

  return (
    'Não foi possível conectar à API. ' +
    'Verifique se ela está ligada.'
  );
}

// Converte datas recebidas da API para o formato brasileiro.
function formatarData(data) {
  if (!data) {
    return 'Não informada';
  }

  const textoData = String(data);
  const partesData = textoData.substring(0, 10).split('-');

  if (partesData.length === 3) {
    return (
      `${partesData[2]}/` +
      `${partesData[1]}/` +
      `${partesData[0]}`
    );
  }

  return textoData;
}

// Formata notas numéricas sem alterar os valores originais.
function formatarNota(valor) {
  if (valor === null || valor === undefined) {
    return 'Não informada';
  }

  const numero = Number(valor);

  if (!Number.isFinite(numero)) {
    return String(valor);
  }

  return numero.toLocaleString('pt-BR', {
    maximumFractionDigits: 2,
  });
}

// Identifica a situação de presença registrada pela API.
function identificarPresenca(valor) {
  if (
    valor === true ||
    valor === 1 ||
    valor === '1'
  ) {
    return 'Presente';
  }

  if (
    valor === false ||
    valor === 0 ||
    valor === '0'
  ) {
    return 'Falta';
  }

  return 'Não informado';
}

// Obtém o primeiro nome para personalizar a saudação.
function obterPrimeiroNome(nomeCompleto) {
  const nome = String(nomeCompleto || '').trim();

  return nome.split(' ')[0] || 'Estudante';
}

// Obtém as iniciais utilizadas no avatar do usuário.
function obterIniciais(nomeCompleto) {
  const partesNome = String(nomeCompleto || '')
    .trim()
    .split(/\s+/)
    .filter(Boolean);

  if (partesNome.length === 0) {
    return 'TA';
  }

  if (partesNome.length === 1) {
    return partesNome[0].substring(0, 2).toUpperCase();
  }

  return (
    partesNome[0][0] +
    partesNome[partesNome.length - 1][0]
  ).toUpperCase();
}

// Botão reutilizável com estados de carregamento e desabilitado.
function Botao({
  titulo,
  aoPressionar,
  desabilitado = false,
  carregando = false,
  secundario = false,
}) {
  return (
    <TouchableOpacity
      style={[
        estilos.botao,
        secundario && estilos.botaoSecundarioClaro,
        desabilitado && estilos.botaoDesabilitado,
      ]}
      onPress={aoPressionar}
      disabled={desabilitado || carregando}
      activeOpacity={0.8}
      accessibilityRole="button"
      accessibilityLabel={titulo}
    >
      {carregando ? (
        <ActivityIndicator
          color={
            secundario
              ? CORES.azulPrincipal
              : CORES.branco
          }
        />
      ) : (
        <Text
          style={[
            estilos.textoBotao,
            secundario && estilos.textoBotaoClaro,
          ]}
        >
          {titulo}
        </Text>
      )}
    </TouchableOpacity>
  );
}

// Exibe um campo informativo com título e valor.
function Informacao({ titulo, valor }) {
  const valorExibido =
    valor === null ||
    valor === undefined ||
    valor === ''
      ? 'Não informado'
      : String(valor);

  return (
    <View style={estilos.informacao}>
      <Text style={estilos.rotuloInformacao}>
        {titulo}
      </Text>

      <Text style={estilos.valorInformacao}>
        {valorExibido}
      </Text>
    </View>
  );
}

// Exibe o avatar do usuário com suas iniciais.
function AvatarUsuario({
  nomeCompleto,
  tamanho = 42,
}) {
  return (
    <View
      style={[
        estilos.avatar,
        {
          width: tamanho,
          height: tamanho,
          borderRadius: tamanho / 2,
        },
      ]}
    >
      <Text
        style={[
          estilos.textoAvatar,
          {
            fontSize: tamanho * 0.34,
          },
        ]}
      >
        {obterIniciais(nomeCompleto)}
      </Text>
    </View>
  );
}

// Representa uma funcionalidade disponível no painel.
function CartaoFuncionalidade({
  simbolo,
  titulo,
  descricao,
  aoPressionar,
  indisponivel = false,
}) {
  return (
    <TouchableOpacity
      style={estilos.cartaoFuncionalidade}
      onPress={aoPressionar}
      activeOpacity={0.75}
      accessibilityRole="button"
      accessibilityLabel={titulo}
    >
      <View style={estilos.iconeFuncionalidade}>
        <Text style={estilos.simboloFuncionalidade}>
          {simbolo}
        </Text>
      </View>

      <View style={estilos.conteudoFuncionalidade}>
        <Text style={estilos.tituloFuncionalidade}>
          {titulo}
        </Text>

        <Text style={estilos.descricaoFuncionalidade}>
          {descricao}
        </Text>

        {indisponivel && (
          <Text style={estilos.textoEmPreparacao}>
            Integração pendente
          </Text>
        )}
      </View>

      <Text style={estilos.setaFuncionalidade}>
        ›
      </Text>
    </TouchableOpacity>
  );
}

// Representa um item da barra de navegação inferior.
function ItemNavegacao({
  simbolo,
  titulo,
  ativo,
  aoPressionar,
}) {
  return (
    <TouchableOpacity
      style={estilos.itemNavegacao}
      onPress={aoPressionar}
      activeOpacity={0.7}
      accessibilityRole="button"
      accessibilityLabel={titulo}
      accessibilityState={{
        selected: ativo,
      }}
    >
      <Text
        style={[
          estilos.iconeNavegacao,
          ativo && estilos.iconeNavegacaoAtivo,
        ]}
      >
        {simbolo}
      </Text>

      <Text
        style={[
          estilos.textoNavegacao,
          ativo && estilos.textoNavegacaoAtivo,
        ]}
      >
        {titulo}
      </Text>
    </TouchableOpacity>
  );
}

// Informa quando uma consulta não possui registros cadastrados.
function EstadoVazio({ titulo, descricao }) {
  return (
    <View style={estilos.estadoVazio}>
      <View style={estilos.iconeEstadoVazio}>
        <Text style={estilos.textoIconeEstadoVazio}>
          i
        </Text>
      </View>

      <Text style={estilos.tituloVazio}>
        {titulo}
      </Text>

      <Text style={estilos.textoAuxiliar}>
        {descricao}
      </Text>
    </View>
  );
}

// Componente principal do aplicativo TechAcademy Mobile.

// Apresenta a evolução acadêmica do aluno autenticado.
function CartaoEvolucaoAluno({ token }) {
  const [progresso, definirProgresso] = useState(null);
  const [carregando, definirCarregando] = useState(true);
  const [erro, definirErro] = useState('');
  const [tentativa, definirTentativa] = useState(0);

  // Consulta os dados reais de XP na API.
  useEffect(() => {
    let ativo = true;

    async function consultarEvolucao() {
      definirCarregando(true);
      definirErro('');
      definirProgresso(null);

      try {
        const { resposta, dadosResposta } = await requisitarApi(
          '/alunos/me/xp',
          {
            method: 'GET',
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        if (!resposta.ok) {
          throw new Error('Falha ao consultar XP.');
        }

        if (
          !Number.isFinite(dadosResposta.xpTotal) ||
          !Number.isFinite(dadosResposta.nivel) ||
          !Number.isFinite(dadosResposta.percentualProgresso)
        ) {
          throw new Error('Dados de evolução inválidos.');
        }

        if (ativo) {
          definirProgresso(dadosResposta);
        }
      } catch {
        if (ativo) {
          definirErro('Não foi possível carregar sua evolução.');
        }
      } finally {
        if (ativo) {
          definirCarregando(false);
        }
      }
    }

    consultarEvolucao();

    return () => {
      ativo = false;
    };
  }, [token, tentativa]);

  const percentual = Math.min(
    100,
    Math.max(0, progresso?.percentualProgresso ?? 0)
  );

  return (
    <View style={[estilos.cartaoDestaque, { minHeight: 132 }]}>

      <View style={{
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
      }}>
        <Text style={estilos.rotuloDestaque}>
          MINHA EVOLUÇÃO
        </Text>

        <Image
          source={require('./assets/simbolo-t-branco.png')}
          style={{ width: 25, height: 25 }}
          resizeMode="contain"
        />
      </View>

      {carregando ? (
        <View style={{
          alignItems: 'center',
          paddingVertical: 20,
        }}>
          <ActivityIndicator color={CORES.branco} />

          <Text style={{
            color: CORES.branco,
            fontSize: 11,
            marginTop: 6,
          }}>
            Carregando evolução...
          </Text>
        </View>

      ) : erro ? (
        <View style={{ paddingVertical: 10 }}>
          <Text style={{
            color: CORES.branco,
            fontSize: 12,
          }}>
            {erro}
          </Text>

          <TouchableOpacity
            onPress={() => definirTentativa(valor => valor + 1)}
            accessibilityRole="button"
          >
            <Text style={{
              color: CORES.branco,
              fontWeight: 'bold',
              marginTop: 8,
            }}>
              Tentar novamente
            </Text>
          </TouchableOpacity>
        </View>

      ) : progresso ? (
        <>
          <View style={{
            flexDirection: 'row',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: 8,
          }}>
            <Text style={{
              color: CORES.branco,
              fontSize: 23,
              fontWeight: 'bold',
            }}>
              Nível {progresso.nivel}
            </Text>

            <Text style={{
              color: CORES.branco,
              fontSize: 19,
              fontWeight: 'bold',
            }}>
              {progresso.xpTotal.toLocaleString('pt-BR')} XP
            </Text>
          </View>

          {/* Barra de progresso do nível atual. */}
          <View
            accessibilityRole="progressbar"
            accessibilityValue={{
              min: 0,
              max: 100,
              now: percentual,
            }}
            style={{
              height: 8,
              borderRadius: 8,
              backgroundColor: 'rgba(255,255,255,0.25)',
              overflow: 'hidden',
            }}
          >
            <View style={{
              height: '100%',
              width: `${percentual}%`,
              borderRadius: 8,
              backgroundColor: CORES.branco,
            }} />
          </View>

          <View style={{
            flexDirection: 'row',
            justifyContent: 'space-between',
            marginTop: 5,
          }}>
            <Text style={{
              color: CORES.branco,
              fontSize: 11,
            }}>
              {progresso.xpNivelAtual.toLocaleString('pt-BR')}
              {' / '}
              {progresso.xpNecessarioPorNivel.toLocaleString('pt-BR')} XP
            </Text>

            <Text style={{
              color: CORES.branco,
              fontSize: 11,
              fontWeight: 'bold',
            }}>
              {percentual}%
            </Text>
          </View>

          <Text style={{
            color: '#DBEAFE',
            fontSize: 11,
            marginTop: 7,
          }}>
            {progresso.atingiuNivelMaximo
              ? 'Parabéns! Você atingiu o nível máximo.'
              : `Faltam ${progresso.xpProximoNivel.toLocaleString('pt-BR')} XP para o próximo nível`}
          </Text>
        </>
      ) : null}
    </View>
  );
}

function Aplicativo() {
  const margensSeguras = useSafeAreaInsets();
  // Estados utilizados na autenticação.
  const [login, definirLogin] = useState('');
  const [senha, definirSenha] = useState('');
  const [senhaVisivel, definirSenhaVisivel] = useState(false);
  const [autenticando, definirAutenticando] = useState(false);

  // Armazena a sessão do usuário autenticado.
  const [sessaoUsuario, definirSessaoUsuario] = useState(null);

  // Armazena a sessão que exige alteração obrigatória de senha.
  const [
    usuarioAlteracaoPendente,
    definirUsuarioAlteracaoPendente,
  ] = useState(null);

  // Estados utilizados na alteração obrigatória de senha.
  const [senhaAtual, definirSenhaAtual] = useState('');
  const [novaSenha, definirNovaSenha] = useState('');

  const [
    confirmarNovaSenha,
    definirConfirmarNovaSenha,
  ] = useState('');

  const [
    senhasAlteracaoVisiveis,
    definirSenhasAlteracaoVisiveis,
  ] = useState(false);

  const [alterandoSenha, definirAlterandoSenha] = useState(false);

  // Identifica a tela selecionada no painel acadêmico.
  const [telaAcademica, definirTelaAcademica] = useState('inicio');

  // Armazena as notas e frequências retornadas pela API.
  const [notasAluno, definirNotasAluno] = useState([]);

  const [
    frequenciasAluno,
    definirFrequenciasAluno,
  ] = useState([]);

  // Mantém disciplinas e turmas reais vinculadas ao aluno autenticado.
  const [disciplinasAluno, definirDisciplinasAluno] = useState([]);
  const [turmasAluno, definirTurmasAluno] = useState([]);

  // Armazena os totais reais da consulta de frequência.
  const [
    resumoFrequencia,
    definirResumoFrequencia,
  ] = useState({
    totalRegistros: 0,
    totalPresencas: 0,
    totalFaltas: 0,
  });

  // Controla o carregamento das consultas acadêmicas.
  const [
    carregandoConsulta,
    definirCarregandoConsulta,
  ] = useState(false);

  const [
    atualizandoConsulta,
    definirAtualizandoConsulta,
  ] = useState(false);

  const [erroConsulta, definirErroConsulta] = useState('');

  // Registra quais consultas já foram carregadas.
  const [
    consultasCarregadas,
    definirConsultasCarregadas,
  ] = useState({
    notas: false,
    frequencias: false,
    disciplinas: false,
    turmas: false,
  });

  // Armazena os vínculos acadêmicos reais do professor autenticado.
  const [vinculosProfessor, definirVinculosProfessor] = useState([]);

  // Dados acadêmicos consultados pelo professor, sempre em modo leitura.
  const [alunosProfessor, definirAlunosProfessor] = useState([]);
  const [notasProfessor, definirNotasProfessor] = useState([]);
  const [frequenciasProfessor, definirFrequenciasProfessor] = useState([]);

  // Controla o carregamento individual das consultas do professor.
  const [consultasProfessorCarregadas, definirConsultasProfessorCarregadas] =
    useState({ alunos: false, notas: false, frequencias: false });

  // Identifica se a consulta do professor já foi concluída.
  const [
    consultaProfessorCarregada,
    definirConsultaProfessorCarregada,
  ] = useState(false);

  // Limpa os campos do formulário de alteração de senha.
  function limparFormularioAlteracaoSenha() {
    definirSenhaAtual('');
    definirNovaSenha('');
    definirConfirmarNovaSenha('');
    definirSenhasAlteracaoVisiveis(false);
  }

  // Remove os dados acadêmicos da sessão anterior.
  function limparDadosAcademicos() {
    definirTelaAcademica('inicio');

    definirNotasAluno([]);
    definirFrequenciasAluno([]);
    definirDisciplinasAluno([]);
    definirTurmasAluno([]);
    definirVinculosProfessor([]);
    definirConsultaProfessorCarregada(false);
    definirAlunosProfessor([]);
    definirNotasProfessor([]);
    definirFrequenciasProfessor([]);
    definirConsultasProfessorCarregadas({
      alunos: false,
      notas: false,
      frequencias: false,
    });

    definirResumoFrequencia({
      totalRegistros: 0,
      totalPresencas: 0,
      totalFaltas: 0,
    });

    definirErroConsulta('');
    definirCarregandoConsulta(false);
    definirAtualizandoConsulta(false);

    definirConsultasCarregadas({
      notas: false,
      frequencias: false,
      disciplinas: false,
      turmas: false,
    });
  }

  // Encerra a sessão e retorna ao formulário de login.
  function realizarLogout() {
    definirSessaoUsuario(null);
    definirUsuarioAlteracaoPendente(null);

    definirLogin('');
    definirSenha('');
    definirSenhaVisivel(false);

    limparFormularioAlteracaoSenha();
    limparDadosAcademicos();
  }
  // Realiza o login utilizando a API Node.js.
  async function realizarLogin() {
    if (autenticando) {
      return;
    }

    const loginInformado = login.trim();

    if (!loginInformado || !senha) {
      Alert.alert(
        'Campos obrigatórios',
        'Informe seu login e sua senha.'
      );

      return;
    }

    definirAutenticando(true);

    try {
      const { resposta, dadosResposta } = await requisitarApi(
        '/auth/login',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            login: loginInformado,
            senha: senha,
          }),
        }
      );

      if (!resposta.ok) {
        Alert.alert(
          'Não foi possível entrar',
          dadosResposta.mensagem ||
            'Verifique suas credenciais.'
        );

        return;
      }

      const usuario = dadosResposta.usuario;
      const token = dadosResposta.token;

      if (!usuario || !token) {
        Alert.alert(
          'Erro de autenticação',
          'A API retornou uma resposta incompleta.'
        );

        return;
      }

      const identificadorPerfil = Number(
        usuario.idPerfil
      );

      // Restringe o acesso Mobile aos perfis Aluno e Professor.
      if (
        identificadorPerfil !== PERFIL_ALUNO &&
        identificadorPerfil !== PERFIL_PROFESSOR
      ) {
        Alert.alert(
          'Acesso não permitido',
          'O aplicativo Mobile é exclusivo para alunos e professores.'
        );

        definirSenha('');
        return;
      }

      // Mantém somente os dados necessários para a sessão.
      const dadosSessao = {
        idUsuario: usuario.idUsuario,
        idPerfil: identificadorPerfil,
        nomeCompleto:
          usuario.nomeCompleto || loginInformado,
        token: token,
      };

      limparDadosAcademicos();

      // Bloqueia o painel enquanto houver alteração obrigatória.
      if (
        usuario.deveAlterarSenha === true ||
        dadosResposta.deveAlterarSenha === true
      ) {
        definirSessaoUsuario(null);
        definirUsuarioAlteracaoPendente(dadosSessao);

        limparFormularioAlteracaoSenha();
        definirSenha('');

        return;
      }

      definirUsuarioAlteracaoPendente(null);
      definirSessaoUsuario(dadosSessao);
      definirSenha('');
    } finally {
      definirAutenticando(false);
    }
  }

  // Valida os requisitos mínimos da nova senha.
  function validarFormularioAlteracaoSenha() {
    if (
      !senhaAtual ||
      !novaSenha ||
      !confirmarNovaSenha
    ) {
      return 'Preencha todos os campos de senha.';
    }

    if (novaSenha !== confirmarNovaSenha) {
      return 'A confirmação não corresponde à nova senha.';
    }

    if (senhaAtual === novaSenha) {
      return 'A nova senha deve ser diferente da senha atual.';
    }

    if (novaSenha.length < 8) {
      return 'A nova senha deve possuir pelo menos 8 caracteres.';
    }

    if (!/[A-Z]/.test(novaSenha)) {
      return 'A nova senha deve conter pelo menos uma letra maiúscula.';
    }

    if (!/[a-z]/.test(novaSenha)) {
      return 'A nova senha deve conter pelo menos uma letra minúscula.';
    }

    if (!/[0-9]/.test(novaSenha)) {
      return 'A nova senha deve conter pelo menos um número.';
    }

    if (!/[^A-Za-z0-9]/.test(novaSenha)) {
      return 'A nova senha deve conter pelo menos um caractere especial.';
    }

    return null;
  }

  // Solicita a alteração de senha à API autenticada.
  async function realizarAlteracaoSenha() {
    if (
      alterandoSenha ||
      !usuarioAlteracaoPendente
    ) {
      return;
    }

    const mensagemValidacao =
      validarFormularioAlteracaoSenha();

    if (mensagemValidacao) {
      Alert.alert(
        'Verifique os campos',
        mensagemValidacao
      );

      return;
    }

    definirAlterandoSenha(true);

    try {
      const { resposta, dadosResposta } = await requisitarApi(
        '/auth/alterar-senha',
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization:
              `Bearer ${usuarioAlteracaoPendente.token}`,
          },
          body: JSON.stringify({
            senhaAtual: senhaAtual,
            novaSenha: novaSenha,
            confirmarNovaSenha: confirmarNovaSenha,
          }),
        }
      );

      if (!resposta.ok) {
        if (resposta.status === 401) {
          realizarLogout();

          Alert.alert(
            'Sessão expirada',
            'Realize o login novamente para alterar sua senha.'
          );

          return;
        }

        Alert.alert(
          'Não foi possível alterar a senha',
          dadosResposta.mensagem ||
            'Verifique os dados informados.'
        );

        return;
      }

      // Libera o painel somente após a confirmação da API.
      if (
        dadosResposta.deveAlterarSenha !== false
      ) {
        Alert.alert(
          'Confirmação pendente',
          'A API não confirmou a liberação do acesso.'
        );

        return;
      }

      limparFormularioAlteracaoSenha();
      limparDadosAcademicos();
      definirSenha('');

      definirSessaoUsuario(
        usuarioAlteracaoPendente
      );

      definirUsuarioAlteracaoPendente(null);

      Alert.alert(
        'Senha alterada',
        'Sua senha foi atualizada com sucesso.'
      );
    } catch (erro) {
      console.error(
        'Falha ao alterar a senha:',
        erro.message
      );

      Alert.alert(
        'Erro de conexão',
        obterMensagemErroConexao(erro)
      );
    } finally {
      definirAlterandoSenha(false);
    }
  }
  // Consulta os dados acadêmicos do aluno autenticado.
  async function carregarConsultaAluno(
    tipoConsulta,
    atualizacao = false
  ) {
    if (
      !sessaoUsuario ||
      sessaoUsuario.idPerfil !== PERFIL_ALUNO
    ) {
      return;
    }

    if (
      carregandoConsulta ||
      atualizandoConsulta
    ) {
      return;
    }

    if (atualizacao) {
      definirAtualizandoConsulta(true);
    } else {
      definirCarregandoConsulta(true);
    }

    definirErroConsulta('');

    // Cada consulta utiliza somente os dados do aluno identificado pelo JWT.
    const caminhosConsulta = {
      notas: '/alunos/me/notas',
      frequencias: '/alunos/me/frequencias',
      disciplinas: '/alunos/me/disciplinas',
      turmas: '/alunos/me/turmas',
    };

    const caminho = caminhosConsulta[tipoConsulta];

    if (!caminho) {
      definirErroConsulta('Consulta acadêmica desconhecida.');
      definirCarregandoConsulta(false);
      definirAtualizandoConsulta(false);
      return;
    }

    try {
      const { resposta, dadosResposta } = await requisitarApi(
        caminho,
        {
          method: 'GET',
          headers: {
            Authorization:
              `Bearer ${sessaoUsuario.token}`,
          },
        }
      );

      // Solicita novo login caso o token tenha expirado.
      if (resposta.status === 401) {
        realizarLogout();

        Alert.alert(
          'Sessão expirada',
          'Realize o login novamente para continuar.'
        );

        return;
      }

      // Impede consultas enquanto a alteração de senha for obrigatória.
      if (
        dadosResposta.codigo ===
        'ALTERACAO_SENHA_OBRIGATORIA'
      ) {
        definirSessaoUsuario(null);
        definirUsuarioAlteracaoPendente(
          sessaoUsuario
        );

        limparFormularioAlteracaoSenha();
        limparDadosAcademicos();

        Alert.alert(
          'Alteração obrigatória',
          'Altere sua senha antes de acessar as consultas.'
        );

        return;
      }

      if (!resposta.ok) {
        definirErroConsulta(
          dadosResposta.mensagem ||
            'Não foi possível consultar os dados.'
        );

        return;
      }

      // Processa as notas retornadas pela API.
      if (tipoConsulta === 'notas') {
        if (!Array.isArray(dadosResposta.notas)) {
          definirErroConsulta(
            'A API retornou uma lista de notas inválida.'
          );

          return;
        }

        definirNotasAluno(dadosResposta.notas);

        definirConsultasCarregadas(
          (estadoAnterior) => ({
            ...estadoAnterior,
            notas: true,
          })
        );
      } else if (tipoConsulta === 'frequencias') {
        // Processa os registros e totais de frequência.
        if (!Array.isArray(dadosResposta.frequencias)) {
          definirErroConsulta(
            'A API retornou uma lista de frequências inválida.'
          );

          return;
        }

        definirFrequenciasAluno(
          dadosResposta.frequencias
        );

        definirResumoFrequencia({
          totalRegistros:
            Number(dadosResposta.totalRegistros) || 0,
          totalPresencas:
            Number(dadosResposta.totalPresencas) || 0,
          totalFaltas:
            Number(dadosResposta.totalFaltas) || 0,
        });

        definirConsultasCarregadas(
          (estadoAnterior) => ({
            ...estadoAnterior,
            frequencias: true,
          })
        );
      } else if (tipoConsulta === 'disciplinas') {
        // Armazena as disciplinas vinculadas à matrícula.
        if (!Array.isArray(dadosResposta.disciplinas)) {
          definirErroConsulta(
            'A API retornou uma lista de disciplinas inválida.'
          );

          return;
        }

        definirDisciplinasAluno(dadosResposta.disciplinas);

        definirConsultasCarregadas(
          (estadoAnterior) => ({
            ...estadoAnterior,
            disciplinas: true,
          })
        );
      } else if (tipoConsulta === 'turmas') {
        // Armazena as turmas associadas ao aluno.
        if (!Array.isArray(dadosResposta.turmas)) {
          definirErroConsulta(
            'A API retornou uma lista de turmas inválida.'
          );

          return;
        }

        definirTurmasAluno(dadosResposta.turmas);

        definirConsultasCarregadas(
          (estadoAnterior) => ({
            ...estadoAnterior,
            turmas: true,
          })
        );
      }
    } catch (erro) {
      console.error(
        'Erro na consulta acadêmica:',
        erro.message
      );

      definirErroConsulta(
        obterMensagemErroConexao(erro)
      );
    } finally {
      definirCarregandoConsulta(false);
      definirAtualizandoConsulta(false);
    }
  }

  // Consulta os vínculos, alunos, notas ou frequências do professor autenticado.
  async function carregarConsultaProfessor(tipoConsulta = 'turmas', atualizacao = false) {
    if (!sessaoUsuario || sessaoUsuario.idPerfil !== PERFIL_PROFESSOR) {
      return;
    }

    if (carregandoConsulta || atualizandoConsulta) {
      return;
    }

    const caminhosConsulta = {
      turmas: '/professores/me/turmas',
      disciplinas: '/professores/me/turmas',
      alunos: '/professores/me/alunos',
      notas: '/professores/me/notas',
      frequencias: '/professores/me/frequencias',
    };

    const caminho = caminhosConsulta[tipoConsulta];

    if (!caminho) {
      definirErroConsulta('Consulta do professor desconhecida.');
      return;
    }

    if (atualizacao) {
      definirAtualizandoConsulta(true);
    } else {
      definirCarregandoConsulta(true);
    }

    definirErroConsulta('');

    try {
      // O backend identifica o professor pelo token JWT.
      const { resposta, dadosResposta } = await requisitarApi(caminho, {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${sessaoUsuario.token}`,
        },
      });

      if (resposta.status === 401) {
        realizarLogout();
        Alert.alert('Sessão expirada', 'Faça login novamente.');
        return;
      }

      if (dadosResposta.codigo === 'ALTERACAO_SENHA_OBRIGATORIA') {
        definirSessaoUsuario(null);
        definirUsuarioAlteracaoPendente(sessaoUsuario);
        limparFormularioAlteracaoSenha();
        limparDadosAcademicos();
        Alert.alert(
          'Alteração obrigatória',
          'Altere sua senha antes de consultar os dados.'
        );
        return;
      }

      if (!resposta.ok) {
        definirErroConsulta(
          dadosResposta.mensagem || 'Não foi possível consultar os dados do professor.'
        );
        return;
      }

      const campoResposta =
        tipoConsulta === 'disciplinas' ? 'turmas' : tipoConsulta;

      const registros = dadosResposta[campoResposta];

      if (!Array.isArray(registros)) {
        definirErroConsulta('A API retornou uma lista de registros inválida.');
        return;
      }

      if (tipoConsulta === 'turmas' || tipoConsulta === 'disciplinas') {
        definirVinculosProfessor(registros);
        definirConsultaProfessorCarregada(true);
      } else {
        const atualizarDados = {
          alunos: definirAlunosProfessor,
          notas: definirNotasProfessor,
          frequencias: definirFrequenciasProfessor,
        };

        atualizarDados[tipoConsulta](registros);
        definirConsultasProfessorCarregadas((anteriores) => ({
          ...anteriores,
          [tipoConsulta]: true,
        }));
      }
    } catch (erro) {
      console.error('Erro na consulta do professor:', erro.message);
      definirErroConsulta(obterMensagemErroConexao(erro));
    } finally {
      definirCarregandoConsulta(false);
      definirAtualizandoConsulta(false);
    }
  }

  // Abre a consulta selecionada e solicita os registros correspondentes.
  function abrirConsultaProfessor(tipoConsulta) {
    definirTelaAcademica(tipoConsulta);
    definirErroConsulta('');
    carregarConsultaProfessor(tipoConsulta);
  }

  // Abre a consulta selecionada e solicita seus dados.
  function abrirConsultaAluno(tipoConsulta) {
    definirTelaAcademica(tipoConsulta);
    definirErroConsulta('');

    carregarConsultaAluno(tipoConsulta);
  }

  // Centraliza a navegação entre as telas do aplicativo.
  function navegarPara(telaDestino) {
    // O professor utiliza as consultas vinculadas ao seu JWT.
    if (
      sessaoUsuario?.idPerfil === PERFIL_PROFESSOR &&
      (
        telaDestino === 'turmas' ||
        telaDestino === 'disciplinas' ||
        telaDestino === 'alunos' ||
        telaDestino === 'notas' ||
        telaDestino === 'frequencias'
      )
    ) {
      abrirConsultaProfessor(telaDestino);
      return;
    }

    if (
      telaDestino === 'notas' ||
      telaDestino === 'frequencias' ||
      telaDestino === 'disciplinas' ||
      telaDestino === 'turmas'
    ) {
      abrirConsultaAluno(telaDestino);
      return;
    }

    definirErroConsulta('');
    definirTelaAcademica(telaDestino);
  }

  // Informa quando uma funcionalidade do professor está pendente.
  function informarFuncionalidadePendente(nomeFuncionalidade) {
    Alert.alert(
      nomeFuncionalidade,
      'Esta consulta será integrada à API do TechAcademy na próxima etapa.'
    );
  }
  // Renderiza a alteração obrigatória de senha.
  if (usuarioAlteracaoPendente) {
    return (
      <KeyboardAvoidingView
        style={[estilos.telaAutenticacao, { paddingTop: margensSeguras.top, paddingBottom: margensSeguras.bottom }]}
        behavior={
          Platform.OS === 'ios'
            ? 'padding'
            : undefined
        }
      >
        <StatusBar
          style="light"
          backgroundColor={CORES.marinho}
        />

        <ScrollView
          contentContainerStyle={
            estilos.conteudoAlteracao
          }
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View style={estilos.cabecalhoAlteracao}>
            <Text style={estilos.nomeAplicativo}>
              TechAcademy
            </Text>

            <Text style={estilos.descricaoAplicativo}>
              Segurança da sua conta
            </Text>
          </View>

          <View style={estilos.cartaoAlteracao}>
            <Text style={estilos.tituloAlteracao}>
              Alterar senha
            </Text>

            <Text style={estilos.descricaoAlteracao}>
              Defina uma nova senha antes de acessar o sistema.
            </Text>

            <Text style={estilos.nomeUsuarioAlteracao}>
              {usuarioAlteracaoPendente.nomeCompleto}
            </Text>

            <Text style={estilos.rotuloCampoAlteracao}>
              Senha atual
            </Text>

            <TextInput
              style={estilos.campoAlteracao}
              placeholder="Digite sua senha atual"
              placeholderTextColor="#8994A7"
              value={senhaAtual}
              onChangeText={definirSenhaAtual}
              secureTextEntry={
                !senhasAlteracaoVisiveis
              }
              autoCapitalize="none"
              autoCorrect={false}
              editable={!alterandoSenha}
              accessibilityLabel="Senha atual"
            />

            <Text style={estilos.rotuloCampoAlteracao}>
              Nova senha
            </Text>

            <TextInput
              style={estilos.campoAlteracao}
              placeholder="Digite sua nova senha"
              placeholderTextColor="#8994A7"
              value={novaSenha}
              onChangeText={definirNovaSenha}
              secureTextEntry={
                !senhasAlteracaoVisiveis
              }
              autoCapitalize="none"
              autoCorrect={false}
              editable={!alterandoSenha}
              accessibilityLabel="Nova senha"
            />

            <Text style={estilos.rotuloCampoAlteracao}>
              Confirmar nova senha
            </Text>

            <TextInput
              style={estilos.campoAlteracao}
              placeholder="Repita sua nova senha"
              placeholderTextColor="#8994A7"
              value={confirmarNovaSenha}
              onChangeText={definirConfirmarNovaSenha}
              secureTextEntry={
                !senhasAlteracaoVisiveis
              }
              autoCapitalize="none"
              autoCorrect={false}
              editable={!alterandoSenha}
              onSubmitEditing={
                realizarAlteracaoSenha
              }
              accessibilityLabel="Confirmar nova senha"
            />

            <TouchableOpacity
              style={estilos.botaoMostrarSenhas}
              onPress={() =>
                definirSenhasAlteracaoVisiveis(
                  (valorAnterior) => !valorAnterior
                )
              }
              disabled={alterandoSenha}
              accessibilityRole="button"
            >
              <Text style={estilos.textoVisibilidadeSenha}>
                {senhasAlteracaoVisiveis
                  ? 'Ocultar senhas'
                  : 'Mostrar senhas'}
              </Text>
            </TouchableOpacity>

            <Text style={estilos.textoRequisitosSenha}>
              Mínimo de 8 caracteres, com maiúscula,
              minúscula, número e caractere especial.
            </Text>

            <Botao
              titulo="Salvar nova senha"
              aoPressionar={realizarAlteracaoSenha}
              desabilitado={alterandoSenha}
              carregando={alterandoSenha}
            />

            <TouchableOpacity
              style={estilos.botaoVoltarLogin}
              onPress={realizarLogout}
              disabled={alterandoSenha}
              accessibilityRole="button"
            >
              <Text style={estilos.textoVoltarLogin}>
                Voltar ao login
              </Text>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    );
  }
  // Renderiza o painel do usuário autenticado.
  if (sessaoUsuario) {
    const usuarioAluno =
      sessaoUsuario.idPerfil === PERFIL_ALUNO;
    const nomePerfil = usuarioAluno
      ? 'Aluno'
      : 'Professor';

    const primeiroNome = obterPrimeiroNome(
      sessaoUsuario.nomeCompleto
    );

    const telaInicio =
      telaAcademica === 'inicio';

    const telaNotas =
      telaAcademica === 'notas';

    const telaFrequencias =
      telaAcademica === 'frequencias';

    const telaDisciplinas =
      telaAcademica === 'disciplinas';

    const telaTurmas =
      telaAcademica === 'turmas';

    const telaAlunos =
      telaAcademica === 'alunos';

    const telaPerfil =
      telaAcademica === 'perfil';

    // Identifica as telas que consultam dados reais da API.
    const telaConsulta =
      telaNotas ||
      telaFrequencias ||
      telaDisciplinas ||
      telaTurmas ||
      telaAlunos;

    return (
      <View style={[estilos.telaPainel, { borderTopWidth: margensSeguras.top, borderTopColor: CORES.marinho }]}>
        <StatusBar
          style="light"
          backgroundColor={CORES.marinho}
        />

        <View style={estilos.barraSuperior}>
          <View style={estilos.identidadeAplicativo}>
            <Image source={require("./assets/simbolo-t-branco.png")} style={{ width: 36, height: 36, borderRadius: 9 }} resizeMode="contain" />

            <View>
              <Text style={estilos.marcaAplicativo}>
                TechAcademy
              </Text>

              <Text style={estilos.subtituloMarca}>
                Portal Acadêmico
              </Text>
            </View>
          </View>

          <TouchableOpacity
            style={estilos.botaoAvatarCabecalho}
            onPress={() => navegarPara('perfil')}
            activeOpacity={0.7}
            accessibilityRole="button"
            accessibilityLabel="Abrir meu perfil"
          >
            <AvatarUsuario
              nomeCompleto={
                sessaoUsuario.nomeCompleto
              }
              tamanho={42}
            />
          </TouchableOpacity>
        </View>

        <ScrollView
          style={estilos.rolagemPainel} scrollEnabled={!telaInicio}
          contentContainerStyle={
            estilos.conteudoPainel
          }
          showsVerticalScrollIndicator={false}
          refreshControl={
            telaConsulta ? (
              <RefreshControl
                refreshing={atualizandoConsulta}
                onRefresh={() =>
                  usuarioAluno
                    ? carregarConsultaAluno(telaAcademica, true)
                    : carregarConsultaProfessor(telaAcademica, true)
                }
                colors={[CORES.azulPrincipal]}
              />
            ) : undefined
          }
        >
          {telaInicio && (
            <>
              <View style={estilos.areaBoasVindas}>
                <Text style={estilos.saudacao}>
                  Olá, {primeiroNome}!
                </Text>

                <Text style={estilos.descricaoSaudacao}>
                  Bem-vindo ao seu espaço acadêmico.
                </Text>
              </View>

              {usuarioAluno ? (
  <CartaoEvolucaoAluno token={sessaoUsuario.token} />
) : (
  <View style={estilos.cartaoDestaque}>
                <View style={estilos.linhaDestaque}>
                  <View style={estilos.conteudoDestaque}>
                    <Text style={estilos.rotuloDestaque}>
                      MEU ESPAÇO
                    </Text>

                    <Text style={estilos.tituloDestaque}>
                      Sua vida acadêmica em um só lugar
                    </Text>

                    <Text style={estilos.descricaoDestaque}>
                      Consulte suas informações de forma simples e organizada.
                    </Text>
                  </View>

                  <Image source={require("./assets/simbolo-t-branco.png")} style={{ width: 48, height: 48, borderRadius: 12 }} resizeMode="contain" />
                </View>

                <View style={estilos.identificacaoDestaque}>
                  <Text style={estilos.textoIdentificacao}>
                    {nomePerfil}
                  </Text>

                  <Text style={estilos.pontoIdentificacao}>
                    •
                  </Text>

                  <Text style={estilos.textoIdentificacao}>
                    Conta autenticada
                  </Text>
                </View>
              </View>
)}
<View style={estilos.cabecalhoSecao}>
                <Text style={estilos.tituloSecao}>
                  Acesso rápido
                </Text>

                <Text style={estilos.descricaoSecao}>
                  Suas principais consultas
                </Text>
              </View>

              {usuarioAluno ? (
                <>
                  <CartaoFuncionalidade
                    simbolo="N"
                    titulo="Minhas notas"
                    descricao="Avaliações e resultados acadêmicos"
                    aoPressionar={() =>
                      abrirConsultaAluno('notas')
                    }
                  />

                  <CartaoFuncionalidade
                    simbolo="F"
                    titulo="Minha frequência"
                    descricao="Presenças, faltas e registros"
                    aoPressionar={() =>
                      abrirConsultaAluno('frequencias')
                    }
                  />

                  <CartaoFuncionalidade
                    simbolo="D"
                    titulo="Minhas disciplinas"
                    descricao="Disciplinas vinculadas à matrícula"
                    aoPressionar={() =>
                      abrirConsultaAluno('disciplinas')
                    }
                  />

                  <CartaoFuncionalidade
                    simbolo="T"
                    titulo="Minha turma"
                    descricao="Informações da turma acadêmica"
                    aoPressionar={() =>
                      abrirConsultaAluno('turmas')
                    }
                  />
                </>
              ) : (
                <>
                  <CartaoFuncionalidade
                    simbolo="T"
                    titulo="Minhas turmas"
                    descricao="Turmas vinculadas ao professor"
                    aoPressionar={() =>
                      abrirConsultaProfessor('turmas')
                    }
                  />

                  <CartaoFuncionalidade
                    simbolo="D"
                    titulo="Minhas disciplinas"
                    descricao="Disciplinas sob responsabilidade"
                    aoPressionar={() =>
                      abrirConsultaProfessor('disciplinas')
                    }
                  />

                  <CartaoFuncionalidade
                    simbolo="A"
                    titulo="Meus alunos"
                    descricao="Alunos matriculados nas suas turmas"
                    aoPressionar={() => abrirConsultaProfessor('alunos')}
                  />

                  <CartaoFuncionalidade
                    simbolo="N"
                    titulo="Notas dos alunos"
                    descricao="Avaliações e notas das suas disciplinas"
                    aoPressionar={() => abrirConsultaProfessor('notas')}
                  />

                  <CartaoFuncionalidade
                    simbolo="F"
                    titulo="Frequência dos alunos"
                    descricao="Presenças e faltas registradas"
                    aoPressionar={() => abrirConsultaProfessor('frequencias')}
                  />
                </>
              )}
            </>
          )}
          {/* Consultas acadêmicas exclusivas do Professor. */}
          {telaConsulta && !usuarioAluno && (
            <>
              <TouchableOpacity
                style={estilos.botaoVoltarPainel}
                onPress={() => navegarPara('inicio')}
                accessibilityRole="button"
              >
                <Text style={estilos.textoVoltarPainel}>
                  ‹ Voltar ao início
                </Text>
              </TouchableOpacity>

              <Text style={estilos.tituloPagina}>
                {telaTurmas
                  ? 'Minhas turmas'
                  : telaDisciplinas
                  ? 'Minhas disciplinas'
                  : telaAlunos
                  ? 'Meus alunos'
                  : telaNotas
                  ? 'Notas dos alunos'
                  : 'Frequência dos alunos'}
              </Text>

              <Text style={estilos.descricaoPagina}>
                Informações vinculadas ao seu cadastro de professor.
              </Text>

              {carregandoConsulta ? (
                <View style={estilos.areaCarregamento}>
                  <ActivityIndicator
                    size="large"
                    color={CORES.azulPrincipal}
                  />

                  <Text style={estilos.textoAuxiliar}>
                    Consultando informações...
                  </Text>
                </View>
              ) : erroConsulta ? (
                <View style={estilos.avisoErro}>
                  <Text style={estilos.textoErro}>
                    {erroConsulta}
                  </Text>
                </View>
              ) : (
                <>
                  {(telaTurmas || telaDisciplinas) && (() => {
                    // Elimina duplicações de turmas ou disciplinas
                    // sem perder os vínculos originais.
                    const identificador = telaTurmas
                      ? 'idTurma'
                      : 'idDisciplina';

                    const itens = Array.from(
                      new Map(
                        vinculosProfessor.map((vinculo) => [
                          vinculo[identificador],
                          vinculo,
                        ])
                      ).values()
                    );

                    return (
                      <>
                        <View style={estilos.cartaoResumo}>
                          <Text style={estilos.rotuloResumo}>
                            {telaTurmas
                              ? 'Turmas vinculadas'
                              : 'Disciplinas vinculadas'}
                          </Text>

                          <Text style={estilos.numeroResumo}>
                            {consultaProfessorCarregada
                              ? itens.length
                              : '—'}
                          </Text>
                        </View>

                        {itens.length === 0 ? (
                          <EstadoVazio
                            titulo="Nenhum vínculo encontrado"
                            descricao="Não existem vínculos ativos para seu perfil."
                          />
                        ) : (
                          itens.map((item, indice) => (
                            <View
                              key={`${telaAcademica}-${indice}`}
                              style={estilos.cartaoRegistro}
                            >
                              <Text style={estilos.tituloRegistro}>
                                {telaTurmas
                                  ? item.nomeTurma
                                  : item.nomeDisciplina}
                              </Text>

                              {telaTurmas ? (
                                <>
                                  <Informacao
                                    titulo="Código da turma"
                                    valor={item.codigoTurma}
                                  />

                                  <Informacao
                                    titulo="Período"
                                    valor={item.periodo}
                                  />

                                  <Informacao
                                    titulo="Ano letivo"
                                    valor={item.anoLetivo}
                                  />

                                  <Informacao
                                    titulo="Semestre"
                                    valor={item.semestre}
                                  />

                                  <Informacao
                                    titulo="Disciplinas da turma"
                                    valor={vinculosProfessor
                                      .filter(
                                        (vinculo) =>
                                          vinculo.idTurma ===
                                          item.idTurma
                                      )
                                      .map(
                                        (vinculo) =>
                                          vinculo.nomeDisciplina
                                      )
                                      .filter(Boolean)
                                      .join(', ')}
                                  />
                                </>
                              ) : (
                                <>
                                  <Informacao
                                    titulo="Código da disciplina"
                                    valor={item.codigoDisciplina}
                                  />

                                  <Informacao
                                    titulo="Turmas vinculadas"
                                    valor={vinculosProfessor
                                      .filter(
                                        (vinculo) =>
                                          vinculo.idDisciplina ===
                                          item.idDisciplina
                                      )
                                      .map(
                                        (vinculo) =>
                                          vinculo.nomeTurma
                                      )
                                      .filter(Boolean)
                                      .join(', ')}
                                  />
                                </>
                              )}
                            </View>
                          ))
                        )}
                      </>
                    );
                  })()}

                  {telaAlunos && (
                    <>
                      <View style={estilos.cartaoResumo}>
                        <Text style={estilos.rotuloResumo}>Alunos matriculados</Text>
                        <Text style={estilos.numeroResumo}>
                          {consultasProfessorCarregadas.alunos
                            ? alunosProfessor.length
                            : '—'}
                        </Text>
                      </View>
                      {alunosProfessor.length === 0 ? (
                        <EstadoVazio
                          titulo="Nenhum aluno encontrado"
                          descricao="Não há alunos com matrícula ativa nas suas turmas."
                        />
                      ) : (
                        alunosProfessor.map((aluno, indice) => (
                          <View
                            key={`${aluno.idMatricula ?? aluno.idAluno}-${indice}`}
                            style={estilos.cartaoRegistro}
                          >
                            <Text style={estilos.tituloRegistro}>
                              {aluno.nomeCompleto || 'Aluno'}
                            </Text>
                            <Informacao titulo="Matrícula" valor={aluno.matriculaAluno} />
                            <Informacao titulo="Turma" valor={aluno.nomeTurma} />
                            <Informacao
                              titulo="Situação da matrícula"
                              valor={aluno.situacaoMatricula}
                            />
                          </View>
                        ))
                      )}
                    </>
                  )}

                  {telaNotas && (
                    <>
                      <View style={estilos.cartaoResumo}>
                        <Text style={estilos.rotuloResumo}>Notas registradas</Text>
                        <Text style={estilos.numeroResumo}>
                          {consultasProfessorCarregadas.notas
                            ? notasProfessor.length
                            : '—'}
                        </Text>
                      </View>
                      {notasProfessor.length === 0 ? (
                        <EstadoVazio
                          titulo="Nenhuma nota encontrada"
                          descricao="Ainda não há notas registradas nas suas disciplinas."
                        />
                      ) : (
                        notasProfessor.map((nota, indice) => (
                          <View
                            key={`${nota.idNota ?? 'nota'}-${indice}`}
                            style={estilos.cartaoRegistro}
                          >
                            <Text style={estilos.tituloRegistro}>
                              {nota.nomeAluno || 'Aluno'}
                            </Text>
                            <Informacao titulo="Matrícula" valor={nota.matriculaAluno} />
                            <Informacao titulo="Disciplina" valor={nota.nomeDisciplina} />
                            <Informacao titulo="Turma" valor={nota.nomeTurma} />
                            <Informacao titulo="Avaliação" valor={nota.tituloAvaliacao} />
                            <Informacao
                              titulo="Nota obtida"
                              valor={formatarNota(nota.valorNota)}
                            />
                            <Informacao
                              titulo="Valor máximo"
                              valor={formatarNota(nota.valorMaximo)}
                            />
                            <Informacao
                              titulo="Data da avaliação"
                              valor={formatarData(nota.dataAvaliacao)}
                            />
                          </View>
                        ))
                      )}
                    </>
                  )}

                  {telaFrequencias && (
                    <>
                      <View style={estilos.gradeResumo}>
                        <View style={estilos.itemResumo}>
                          <Text style={estilos.numeroResumo}>
                            {frequenciasProfessor.length}
                          </Text>
                          <Text style={estilos.rotuloResumo}>Registros</Text>
                        </View>
                        <View style={estilos.itemResumo}>
                          <Text style={estilos.numeroResumo}>
                            {frequenciasProfessor.filter(
                              (item) => identificarPresenca(item.presente) === 'Presente'
                            ).length}
                          </Text>
                          <Text style={estilos.rotuloResumo}>Presenças</Text>
                        </View>
                        <View style={estilos.itemResumo}>
                          <Text style={estilos.numeroResumo}>
                            {frequenciasProfessor.filter(
                              (item) => identificarPresenca(item.presente) === 'Falta'
                            ).length}
                          </Text>
                          <Text style={estilos.rotuloResumo}>Faltas</Text>
                        </View>
                      </View>
                      {frequenciasProfessor.length === 0 ? (
                        <EstadoVazio
                          titulo="Nenhuma frequência encontrada"
                          descricao="Ainda não há presenças ou faltas registradas nas suas disciplinas."
                        />
                      ) : (
                        frequenciasProfessor.map((frequencia, indice) => (
                          <View
                            key={`${frequencia.idFrequencia ?? 'frequencia'}-${indice}`}
                            style={estilos.cartaoRegistro}
                          >
                            <Text style={estilos.tituloRegistro}>
                              {frequencia.nomeAluno || 'Aluno'}
                            </Text>
                            <Informacao
                              titulo="Presença"
                              valor={identificarPresenca(frequencia.presente)}
                            />
                            <Informacao titulo="Matrícula" valor={frequencia.matriculaAluno} />
                            <Informacao titulo="Disciplina" valor={frequencia.nomeDisciplina} />
                            <Informacao titulo="Turma" valor={frequencia.nomeTurma} />
                            <Informacao
                              titulo="Data da aula"
                              valor={formatarData(frequencia.dataAula)}
                            />
                            <Informacao titulo="Conteúdo da aula" valor={frequencia.conteudoAula} />
                          </View>
                        ))
                      )}
                    </>
                  )}
                </>
              )}

              <View style={estilos.areaAtualizar}>
                <Botao
                  titulo="Atualizar consulta"
                  secundario
                  aoPressionar={() =>
                    carregarConsultaProfessor(telaAcademica)
                  }
                />
              </View>
            </>
          )}

          {telaConsulta && usuarioAluno && (
            <>
              <TouchableOpacity
                style={estilos.botaoVoltarPainel}
                onPress={() =>
                  navegarPara('inicio')
                }
                accessibilityRole="button"
              >
                <Text style={estilos.textoVoltarPainel}>
                  ‹ Voltar ao início
                </Text>
              </TouchableOpacity>

              <Text style={estilos.tituloPagina}>
                {telaNotas
                  ? 'Minhas notas'
                  : telaFrequencias
                  ? 'Minha frequência'
                  : telaDisciplinas
                  ? 'Minhas disciplinas'
                  : 'Minha turma'}
              </Text>

              <Text style={estilos.descricaoPagina}>
                {telaNotas
                  ? 'Acompanhe suas avaliações e resultados.'
                  : telaFrequencias
                  ? 'Consulte seus registros de presença e falta.'
                  : telaDisciplinas
                  ? 'Consulte as disciplinas vinculadas à sua matrícula.'
                  : 'Consulte as informações da sua turma acadêmica.'}
              </Text>

              {carregandoConsulta ? (
                <View style={estilos.areaCarregamento}>
                  <ActivityIndicator
                    size="large"
                    color={CORES.azulPrincipal}
                  />

                  <Text style={estilos.textoAuxiliar}>
                    Consultando informações...
                  </Text>
                </View>
              ) : (
                <>
                  {erroConsulta ? (
                    <View style={estilos.avisoErro}>
                      <Text style={estilos.textoErro}>
                        {erroConsulta}
                      </Text>
                    </View>
                  ) : (
                    <>
                      {telaNotas && (
                        <>
                          <View style={estilos.cartaoResumo}>
                            <Text style={estilos.rotuloResumo}>
                              Avaliações registradas
                            </Text>

                            <Text style={estilos.numeroResumo}>
                              {consultasCarregadas.notas
                                ? notasAluno.length
                                : '—'}
                            </Text>
                          </View>

                          {notasAluno.length === 0 ? (
                            <EstadoVazio
                              titulo="Nenhuma nota encontrada"
                              descricao={
                                'Ainda não existem avaliações ' +
                                'com notas lançadas para sua matrícula.'
                              }
                            />
                          ) : (
                            notasAluno.map(
                              (nota, indice) => (
                                <View
                                  key={`${nota.idNota ?? 'nota'}-${indice}`}
                                  style={estilos.cartaoRegistro}
                                >
                                  <Text style={estilos.tituloRegistro}>
                                    {nota.disciplina ||
                                      'Disciplina'}
                                  </Text>

                                  <Informacao
                                    titulo="Avaliação"
                                    valor={nota.avaliacao}
                                  />
                                  <Informacao
                                    titulo="Nota obtida"
                                    valor={formatarNota(
                                      nota.valorNota
                                    )}
                                  />

                                  <Informacao
                                    titulo="Valor máximo"
                                    valor={formatarNota(
                                      nota.valorMaximo
                                    )}
                                  />

                                  <Informacao
                                    titulo="Turma"
                                    valor={nota.turma}
                                  />

                                  <Informacao
                                    titulo="Data da avaliação"
                                    valor={formatarData(
                                      nota.dataAvaliacao
                                    )}
                                  />

                                  <Informacao
                                    titulo="Situação da matrícula"
                                    valor={
                                      nota.situacaoMatricula
                                    }
                                  />
                                </View>
                              )
                            )
                          )}
                        </>
                      )}

                      {telaFrequencias && (
                        <>
                          <View style={estilos.gradeResumo}>
                            <View style={estilos.itemResumo}>
                              <Text style={estilos.numeroResumo}>
                                {
                                  resumoFrequencia.totalRegistros
                                }
                              </Text>

                              <Text style={estilos.rotuloResumo}>
                                Registros
                              </Text>
                            </View>

                            <View style={estilos.itemResumo}>
                              <Text style={estilos.numeroResumo}>
                                {
                                  resumoFrequencia.totalPresencas
                                }
                              </Text>

                              <Text style={estilos.rotuloResumo}>
                                Presenças
                              </Text>
                            </View>

                            <View style={estilos.itemResumo}>
                              <Text style={estilos.numeroResumo}>
                                {
                                  resumoFrequencia.totalFaltas
                                }
                              </Text>

                              <Text style={estilos.rotuloResumo}>
                                Faltas
                              </Text>
                            </View>
                          </View>

                          {frequenciasAluno.length === 0 ? (
                            <EstadoVazio
                              titulo="Nenhuma frequência encontrada"
                              descricao={
                                'Ainda não existem registros ' +
                                'de presença ou falta para sua matrícula.'
                              }
                            />
                          ) : (
                            frequenciasAluno.map(
                              (frequencia, indice) => (
                                <View
                                  key={`${frequencia.idFrequencia ?? 'frequencia'}-${indice}`}
                                  style={estilos.cartaoRegistro}
                                >
                                  <Text style={estilos.tituloRegistro}>
                                    {identificarPresenca(
                                      frequencia.presente
                                    )}
                                  </Text>

                                  <Informacao
                                    titulo="Turma"
                                    valor={
                                      frequencia.turma
                                    }
                                  />

                                  <Informacao
                                    titulo="Data do registro"
                                    valor={formatarData(
                                      frequencia.dataRegistro
                                    )}
                                  />

                                  <Informacao
                                    titulo="Identificador da aula"
                                    valor={
                                      frequencia.idAula
                                    }
                                  />

                                  <Informacao
                                    titulo="Situação da matrícula"
                                    valor={
                                      frequencia.situacaoMatricula
                                    }
                                  />
                                </View>
                              )
                            )
                          )}
                        </>
                      )}

                      {telaDisciplinas && (
                        <>
                          <View style={estilos.cartaoResumo}>
                            <Text style={estilos.rotuloResumo}>
                              Disciplinas vinculadas
                            </Text>

                            <Text style={estilos.numeroResumo}>
                              {consultasCarregadas.disciplinas
                                ? disciplinasAluno.length
                                : '—'}
                            </Text>
                          </View>

                          {disciplinasAluno.length === 0 ? (
                            <EstadoVazio
                              titulo="Nenhuma disciplina encontrada"
                              descricao="Não existem disciplinas vinculadas à sua matrícula."
                            />
                          ) : (
                            disciplinasAluno.map(
                              (disciplina, indice) => (
                                <View
                                  key={`${disciplina.idDisciplina}-${disciplina.idTurma}-${indice}`}
                                  style={estilos.cartaoRegistro}
                                >
                                  <Text style={estilos.tituloRegistro}>
                                    {disciplina.nomeDisciplina ||
                                      'Disciplina'}
                                  </Text>

                                  <Informacao
                                    titulo="Código da disciplina"
                                    valor={
                                      disciplina.codigoDisciplina
                                    }
                                  />

                                  <Informacao
                                    titulo="Carga horária"
                                    valor={
                                      disciplina.cargaHoraria != null
                                        ? `${disciplina.cargaHoraria} horas`
                                        : null
                                    }
                                  />

                                  <Informacao
                                    titulo="Turma"
                                    valor={
                                      disciplina.nomeTurma
                                    }
                                  />

                                  <Informacao
                                    titulo="Código da turma"
                                    valor={
                                      disciplina.codigoTurma
                                    }
                                  />

                                  <Informacao
                                    titulo="Situação da matrícula"
                                    valor={
                                      disciplina.situacaoMatricula
                                    }
                                  />
                                </View>
                              )
                            )
                          )}
                        </>
                      )}

                      {telaTurmas && (
                        <>
                          <View style={estilos.cartaoResumo}>
                            <Text style={estilos.rotuloResumo}>
                              Turmas vinculadas
                            </Text>

                            <Text style={estilos.numeroResumo}>
                              {consultasCarregadas.turmas
                                ? turmasAluno.length
                                : '—'}
                            </Text>
                          </View>

                          {turmasAluno.length === 0 ? (
                            <EstadoVazio
                              titulo="Nenhuma turma encontrada"
                              descricao="Não existem turmas vinculadas à sua matrícula."
                            />
                          ) : (
                            turmasAluno.map(
                              (turma, indice) => (
                                <View
                                  key={`${turma.idMatricula ?? turma.idTurma}-${indice}`}
                                  style={estilos.cartaoRegistro}
                                >
                                  <Text style={estilos.tituloRegistro}>
                                    {turma.nomeTurma ||
                                      'Turma acadêmica'}
                                  </Text>

                                  <Informacao
                                    titulo="Código da turma"
                                    valor={turma.codigoTurma}
                                  />

                                  <Informacao
                                    titulo="Período"
                                    valor={turma.periodo}
                                  />

                                  <Informacao
                                    titulo="Ano letivo"
                                    valor={turma.anoLetivo}
                                  />

                                  <Informacao
                                    titulo="Semestre"
                                    valor={turma.semestre}
                                  />

                                  <Informacao
                                    titulo="Situação da matrícula"
                                    valor={
                                      turma.situacaoMatricula
                                    }
                                  />
                                </View>
                              )
                            )
                          )}
                        </>
                      )}
                    </>
                  )}

                  <View style={estilos.areaAtualizar}>
                    <Botao
                      titulo="Atualizar consulta"
                      aoPressionar={() =>
                        carregarConsultaAluno(
                          telaAcademica
                        )
                      }
                      secundario
                    />
                  </View>
                </>
              )}
            </>
          )}
          {telaPerfil && (
            <>
              <Text style={estilos.tituloPagina}>
                Meu perfil
              </Text>

              <Text style={estilos.descricaoPagina}>
                Informações da sua conta TechAcademy.
              </Text>

              <View style={estilos.cartaoPerfil}>
                <AvatarUsuario
                  nomeCompleto={
                    sessaoUsuario.nomeCompleto
                  }
                  tamanho={72}
                />

                <Text style={estilos.nomePerfil}>
                  {sessaoUsuario.nomeCompleto}
                </Text>

                <Text style={estilos.tipoPerfil}>
                  {nomePerfil}
                </Text>
              </View>

              <View style={estilos.cartaoInformacoes}>
                <Text style={estilos.tituloInformacoes}>
                  Dados da conta
                </Text>

                <Informacao
                  titulo="Nome completo"
                  valor={
                    sessaoUsuario.nomeCompleto
                  }
                />

                <Informacao
                  titulo="Perfil de acesso"
                  valor={nomePerfil}
                />

                <Informacao
                  titulo="Identificador do usuário"
                  valor={
                    sessaoUsuario.idUsuario
                  }
                />
              </View>

              <View style={estilos.areaSair}>
                <Botao
                  titulo="Sair da conta"
                  aoPressionar={realizarLogout}
                  secundario
                />
              </View>
            </>
          )}

          <Text style={estilos.rodapePainel}>
            © 2026 TechAcademy
          </Text>
        </ScrollView>

        <View style={[estilos.barraNavegacao, { paddingBottom: margensSeguras.bottom + 8 }]}>
          <ItemNavegacao
            simbolo="⌂"
            titulo="Início"
            ativo={telaInicio}
            aoPressionar={() =>
              navegarPara('inicio')
            }
          />

          {usuarioAluno ? (
            <>
              <ItemNavegacao
                simbolo="N"
                titulo="Notas"
                ativo={telaNotas}
                aoPressionar={() =>
                  navegarPara('notas')
                }
              />

              <ItemNavegacao
                simbolo="F"
                titulo="Frequência"
                ativo={telaFrequencias}
                aoPressionar={() =>
                  navegarPara('frequencias')
                }
              />
            </>
          ) : (
            <>
              <ItemNavegacao
                simbolo="T"
                titulo="Turmas"
                ativo={telaTurmas}
                aoPressionar={() => navegarPara('turmas')}
              />

              <ItemNavegacao
                simbolo="D"
                titulo="Disciplinas"
                ativo={telaDisciplinas}
                aoPressionar={() => navegarPara('disciplinas')}
              />
            </>
          )}

          <ItemNavegacao
            simbolo="●"
            titulo="Perfil"
            ativo={telaPerfil}
            aoPressionar={() =>
              navegarPara('perfil')
            }
          />
        </View>
      </View>
    );
  }

  // Renderiza o login quando não existe sessão autenticada.
  return (
    <KeyboardAvoidingView
      style={[estilos.telaAutenticacao, { paddingTop: margensSeguras.top, paddingBottom: margensSeguras.bottom }]}
      behavior={
        Platform.OS === 'ios'
          ? 'padding'
          : undefined
      }
    >
      <StatusBar
        style="light"
        backgroundColor={CORES.marinho}
      />

      <ScrollView
        contentContainerStyle={
          estilos.conteudoLogin
        }
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
      >
        <View style={estilos.cabecalhoLogin}>
          <Image source={require("./assets/simbolo-t-branco.png")} style={{ width: 64, height: 64, borderRadius: 16 }} resizeMode="contain" />

          <Text style={estilos.nomeAplicativo}>
            TechAcademy
          </Text>

          <Text style={estilos.descricaoAplicativo}>
            Sua jornada acadêmica em um só lugar
          </Text>
        </View>

        <View style={estilos.cartaoLogin}>
          <Text style={estilos.tituloLogin}>
            Bem-vindo!
          </Text>

          <Text style={estilos.descricaoLogin}>
            Entre com suas credenciais para continuar.
          </Text>

          <Text style={estilos.rotuloCampo}>
            Login
          </Text>

          <TextInput
            style={estilos.campoTexto}
            placeholder="Digite seu login"
            placeholderTextColor="#8994A7"
            value={login}
            onChangeText={definirLogin}
            autoCapitalize="none"
            autoCorrect={false}
            autoComplete="username"
            returnKeyType="next"
            editable={!autenticando}
            accessibilityLabel="Login do usuário"
          />

          <Text style={estilos.rotuloCampo}>
            Senha
          </Text>

          <View style={estilos.conteinerSenha}>
            <TextInput
              style={estilos.campoSenha}
              placeholder="Digite sua senha"
              placeholderTextColor="#8994A7"
              value={senha}
              onChangeText={definirSenha}
              secureTextEntry={!senhaVisivel}
              autoComplete="password"
              autoCapitalize="none"
              autoCorrect={false}
              returnKeyType="done"
              editable={!autenticando}
              onSubmitEditing={realizarLogin}
              accessibilityLabel="Senha do usuário"
            />

            <TouchableOpacity
              style={estilos.botaoVisibilidadeSenha}
              onPress={() =>
                definirSenhaVisivel(
                  (valorAnterior) => !valorAnterior
                )
              }
              accessibilityRole="button"
              accessibilityLabel={
                senhaVisivel
                  ? 'Ocultar senha'
                  : 'Mostrar senha'
              }
            >
              <Text style={estilos.textoVisibilidadeSenha}>
                {senhaVisivel
                  ? 'Ocultar'
                  : 'Mostrar'}
              </Text>
            </TouchableOpacity>
          </View>

          <Botao
            titulo="Entrar"
            aoPressionar={realizarLogin}
            desabilitado={autenticando}
            carregando={autenticando}
          />

          <Text style={estilos.informacaoAcesso}>
            Acesso exclusivo para alunos e professores
          </Text>
        </View>

        <Text style={estilos.rodapeLogin}>
          © 2026 TechAcademy
        </Text>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
const estilos = StyleSheet.create({
  telaAutenticacao: {
    flex: 1,
    backgroundColor: CORES.marinho,
  },

  telaPainel: {
    flex: 1,
    backgroundColor: CORES.fundo,
  },

  conteudoLogin: {
    flexGrow: 1,
    justifyContent: 'center',
    paddingHorizontal: 22,
    paddingVertical: 45,
  },

  cabecalhoLogin: {
    alignItems: 'center',
    marginBottom: 30,
  },

  logotipoLogin: {
    width: 70,
    height: 70,
    borderRadius: 20,
    backgroundColor: '#3979EF',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 16,
  },

  letraLogotipo: {
    fontSize: 39,
    fontWeight: '800',
    color: CORES.branco,
  },

  nomeAplicativo: {
    fontSize: 30,
    fontWeight: '800',
    color: CORES.branco,
    textAlign: 'center',
  },

  descricaoAplicativo: {
    color: '#CBD5E1',
    fontSize: 14,
    textAlign: 'center',
    marginTop: 8,
  },

  cartaoLogin: {
    backgroundColor: CORES.branco,
    borderRadius: 22,
    padding: 24,
    width: '100%',
    maxWidth: 430,
    alignSelf: 'center',
    elevation: 5,
  },

  tituloLogin: {
    color: CORES.textoPrincipal,
    fontSize: 25,
    fontWeight: '800',
    textAlign: 'center',
  },

  descricaoLogin: {
    color: CORES.textoSecundario,
    fontSize: 13,
    textAlign: 'center',
    marginTop: 8,
    marginBottom: 26,
    lineHeight: 20,
  },

  rotuloCampo: {
    fontSize: 14,
    fontWeight: '600',
    color: '#26364F',
    marginBottom: 8,
  },

  campoTexto: {
    height: 52,
    borderWidth: 1,
    borderColor: '#D9E1EC',
    borderRadius: 12,
    paddingHorizontal: 15,
    color: CORES.textoPrincipal,
    backgroundColor: '#F8FAFC',
    marginBottom: 20,
    fontSize: 15,
  },

  conteinerSenha: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#D9E1EC',
    borderRadius: 12,
    backgroundColor: '#F8FAFC',
    marginBottom: 25,
  },

  campoSenha: {
    flex: 1,
    minWidth: 0,
    height: 52,
    paddingHorizontal: 15,
    fontSize: 15,
    color: CORES.textoPrincipal,
  },

  botaoVisibilidadeSenha: {
    paddingHorizontal: 12,
    paddingVertical: 12,
  },

  textoVisibilidadeSenha: {
    fontSize: 12,
    fontWeight: '600',
    color: '#3979EF',
  },

  botao: {
    backgroundColor: '#3979EF',
    minHeight: 50,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 12,
  },

  botaoSecundarioClaro: {
    backgroundColor: CORES.azulClaro,
    borderWidth: 1,
    borderColor: CORES.azulBorda,
  },

  botaoDesabilitado: {
    opacity: 0.65,
  },

  textoBotao: {
    color: CORES.branco,
    fontSize: 15,
    fontWeight: '700',
  },

  textoBotaoClaro: {
    color: CORES.azulPrincipal,
  },

  informacaoAcesso: {
    fontSize: 12,
    color: '#8490A3',
    textAlign: 'center',
    marginTop: 20,
  },

  rodapeLogin: {
    textAlign: 'center',
    color: '#AAB8D1',
    fontSize: 12,
    marginTop: 30,
  },

  // Estilos da alteração obrigatória de senha.
  conteudoAlteracao: {
    flexGrow: 1,
    justifyContent: 'center',
    paddingHorizontal: 20,
    paddingTop: 12,
    paddingBottom: 24,
  },

  cabecalhoAlteracao: {
    alignItems: 'center',
    marginBottom: 14,
  },

  cartaoAlteracao: {
    backgroundColor: CORES.branco,
    borderRadius: 20,
    paddingHorizontal: 20,
    paddingTop: 18,
    paddingBottom: 10,
    width: '100%',
    maxWidth: 430,
    alignSelf: 'center',
    elevation: 5,
  },

  tituloAlteracao: {
    color: CORES.textoPrincipal,
    fontSize: 23,
    fontWeight: '800',
    textAlign: 'center',
  },

  descricaoAlteracao: {
    color: CORES.textoSecundario,
    fontSize: 12,
    textAlign: 'center',
    lineHeight: 17,
    marginTop: 5,
    marginBottom: 10,
  },

  nomeUsuarioAlteracao: {
    color: '#3979EF',
    fontSize: 14,
    fontWeight: '700',
    textAlign: 'center',
    marginBottom: 13,
  },

  rotuloCampoAlteracao: {
    fontSize: 13,
    fontWeight: '600',
    color: '#26364F',
    marginBottom: 5,
  },

  campoAlteracao: {
    height: 44,
    borderWidth: 1,
    borderColor: '#D9E1EC',
    borderRadius: 11,
    paddingHorizontal: 13,
    color: CORES.textoPrincipal,
    backgroundColor: '#F8FAFC',
    marginBottom: 11,
    fontSize: 14,
  },

  botaoMostrarSenhas: {
    alignSelf: 'flex-end',
    paddingVertical: 5,
    marginTop: -5,
    marginBottom: 8,
  },

  textoRequisitosSenha: {
    fontSize: 11,
    color: CORES.textoSecundario,
    lineHeight: 16,
    marginBottom: 13,
  },

  botaoVoltarLogin: {
    minHeight: 38,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 5,
  },

  textoVoltarLogin: {
    color: '#3979EF',
    fontSize: 13,
    fontWeight: '700',
  },

  // Cabeçalho do painel acadêmico.
  barraSuperior: {
    backgroundColor: CORES.marinho,
    paddingHorizontal: 20,
    paddingTop: 9,
    paddingBottom: 11,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },

  identidadeAplicativo: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },

  logotipoPequeno: {
    width: 40,
    height: 40,
    borderRadius: 12,
    backgroundColor: CORES.azulPrincipal,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 10,
  },

  letraLogotipoPequeno: {
    fontSize: 24,
    fontWeight: '800',
    color: CORES.branco,
  },

  marcaAplicativo: {
    fontSize: 17,
    fontWeight: '800',
    color: CORES.branco,
  },

  subtituloMarca: {
    fontSize: 10,
    color: '#CBD5E1',
    marginTop: 2,
  },

  // Mantém o avatar superior com uma área de toque confortável.
  botaoAvatarCabecalho: {
    width: 56,
    height: 56,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: -6,
  },

  avatar: {
    backgroundColor: CORES.azulClaro,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 2,
    borderColor: CORES.azulBorda,
  },

  textoAvatar: {
    color: CORES.azulEscuro,
    fontWeight: '800',
  },

  // Ajusta a distribuição vertical do dashboard.
  rolagemPainel: {
    flex: 1,
  },

  conteudoPainel: {
    flexGrow: 1,
    paddingHorizontal: 20,
    paddingTop: 20,
    paddingBottom: 18,
  },

  areaBoasVindas: {
    marginBottom: 10,
  },

  saudacao: {
    fontSize: 25,
    fontWeight: '800',
    color: CORES.textoPrincipal,
  },

  descricaoSaudacao: {
    fontSize: 13,
    color: CORES.textoSecundario,
    marginTop: 5,
  },

  // Aumenta moderadamente o cartão azul principal.
  cartaoDestaque: {
    backgroundColor: CORES.azulPrincipal,
    borderRadius: 18,
    padding: 12,
    marginBottom: 12,
    elevation: 3,
  },

  linhaDestaque: {
    flexDirection: 'row',
    alignItems: 'center',
  },

  conteudoDestaque: {
    flex: 1,
  },

  rotuloDestaque: {
    color: '#BFDBFE',
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 1,
    marginBottom: 7,
  },

  tituloDestaque: {
    color: CORES.branco,
    fontSize: 18,
    fontWeight: '800',
    lineHeight: 23,
  },

  descricaoDestaque: {
    color: '#DBEAFE',
    fontSize: 12,
    lineHeight: 17,
    marginTop: 8,
  },

  iconeDestaque: {
    width: 43,
    height: 43,
    borderRadius: 13,
    backgroundColor: 'rgba(255,255,255,0.15)',
    alignItems: 'center',
    justifyContent: 'center',
    marginLeft: 9,
  },

  textoIconeDestaque: {
    color: CORES.branco,
    fontSize: 24,
    fontWeight: '800',
  },

  identificacaoDestaque: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 14,
  },

  textoIdentificacao: {
    color: CORES.branco,
    fontSize: 10,
    fontWeight: '600',
  },

  pontoIdentificacao: {
    color: '#BFDBFE',
    marginHorizontal: 8,
  },

  // Dá mais espaço ao cabeçalho das funcionalidades.
  cabecalhoSecao: {
    marginBottom: 13,
  },

  tituloSecao: {
    color: CORES.textoPrincipal,
    fontSize: 18,
    fontWeight: '800',
  },

  descricaoSecao: {
    color: CORES.textoSecundario,
    fontSize: 12,
    marginTop: 4,
  },

  // Amplia os cartões acadêmicos sem ocupar a tela inteira.
  cartaoFuncionalidade: {
    backgroundColor: CORES.branco,
    borderRadius: 15,
    borderWidth: 1,
    borderColor: CORES.borda,
    padding: 10,
    marginBottom: 14,
    minHeight: 58,
    flexDirection: 'row',
    alignItems: 'center',
  },

  iconeFuncionalidade: {
    width: 44,
    height: 44,
    borderRadius: 12,
    backgroundColor: CORES.azulClaro,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },

  simboloFuncionalidade: {
    color: CORES.azulPrincipal,
    fontSize: 20,
    fontWeight: '800',
  },

  conteudoFuncionalidade: {
    flex: 1,
  },

  tituloFuncionalidade: {
    color: CORES.textoPrincipal,
    fontSize: 15,
    fontWeight: '700',
  },

  descricaoFuncionalidade: {
    color: CORES.textoSecundario,
    fontSize: 11,
    marginTop: 4,
    lineHeight: 15,
  },

  textoEmPreparacao: {
    color: CORES.azulPrincipal,
    fontSize: 10,
    fontWeight: '700',
    marginTop: 5,
  },

  setaFuncionalidade: {
    color: CORES.textoDiscreto,
    fontSize: 27,
    marginLeft: 8,
  },

  // Barra inferior fixa durante a navegação.
  barraNavegacao: {
    backgroundColor: CORES.branco,
    borderTopWidth: 1,
    borderTopColor: CORES.borda,
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingTop: 8,
    paddingBottom: 10,
  },

  itemNavegacao: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 43,
  },

  iconeNavegacao: {
    fontSize: 20,
    color: CORES.textoDiscreto,
    fontWeight: '700',
  },

  iconeNavegacaoAtivo: {
    color: CORES.azulPrincipal,
  },

  textoNavegacao: {
    fontSize: 10,
    fontWeight: '600',
    color: CORES.textoDiscreto,
    marginTop: 3,
  },

  textoNavegacaoAtivo: {
    color: CORES.azulPrincipal,
    fontWeight: '800',
  },

  // Títulos e controles das consultas acadêmicas.
  botaoVoltarPainel: {
    alignSelf: 'flex-start',
    paddingVertical: 7,
    marginBottom: 15,
  },

  textoVoltarPainel: {
    color: CORES.azulPrincipal,
    fontSize: 14,
    fontWeight: '700',
  },

  tituloPagina: {
    color: CORES.textoPrincipal,
    fontSize: 25,
    fontWeight: '800',
  },

  descricaoPagina: {
    color: CORES.textoSecundario,
    fontSize: 13,
    lineHeight: 20,
    marginTop: 7,
    marginBottom: 22,
  },

  areaCarregamento: {
    alignItems: 'center',
    paddingVertical: 45,
  },

  textoAuxiliar: {
    color: CORES.textoSecundario,
    fontSize: 13,
    lineHeight: 20,
    textAlign: 'center',
    marginTop: 12,
  },

  avisoErro: {
    backgroundColor: CORES.vermelhoClaro,
    borderRadius: 14,
    padding: 16,
    marginBottom: 18,
  },

  textoErro: {
    color: CORES.vermelho,
    fontSize: 13,
    lineHeight: 20,
  },

  // Resumos das notas e frequências.
  cartaoResumo: {
    backgroundColor: CORES.branco,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: CORES.borda,
    padding: 20,
    alignItems: 'center',
    marginBottom: 18,
  },

  gradeResumo: {
    flexDirection: 'row',
    marginBottom: 18,
    gap: 8,
  },

  itemResumo: {
    flex: 1,
    backgroundColor: CORES.branco,
    borderRadius: 14,
    borderWidth: 1,
    borderColor: CORES.borda,
    paddingVertical: 17,
    paddingHorizontal: 5,
    alignItems: 'center',
  },

  numeroResumo: {
    color: CORES.azulPrincipal,
    fontSize: 27,
    fontWeight: '800',
  },

  rotuloResumo: {
    color: CORES.textoSecundario,
    fontSize: 11,
    fontWeight: '600',
    textAlign: 'center',
    marginBottom: 5,
  },

  // Mensagem exibida quando não existem registros.
  estadoVazio: {
    backgroundColor: CORES.branco,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: CORES.borda,
    padding: 25,
    alignItems: 'center',
    marginBottom: 18,
  },

  iconeEstadoVazio: {
    width: 46,
    height: 46,
    borderRadius: 23,
    backgroundColor: CORES.azulClaro,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 15,
  },

  textoIconeEstadoVazio: {
    color: CORES.azulPrincipal,
    fontSize: 24,
    fontWeight: '800',
    textAlign: 'center',
  },

  tituloVazio: {
    color: CORES.textoPrincipal,
    fontSize: 15,
    fontWeight: '800',
    textAlign: 'center',
  },

  // Cartões dos registros acadêmicos.
  cartaoRegistro: {
    backgroundColor: CORES.branco,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: CORES.borda,
    padding: 18,
    marginBottom: 14,
  },

  tituloRegistro: {
    color: CORES.textoPrincipal,
    fontSize: 17,
    fontWeight: '800',
    marginBottom: 15,
  },

  informacao: {
    marginBottom: 14,
  },

  rotuloInformacao: {
    color: CORES.textoSecundario,
    fontSize: 12,
    fontWeight: '600',
    marginBottom: 4,
  },

  valorInformacao: {
    color: CORES.textoPrincipal,
    fontSize: 15,
    fontWeight: '600',
  },

  areaAtualizar: {
    marginTop: 6,
    marginBottom: 12,
  },

  // Cartões da página de perfil.
  cartaoPerfil: {
    backgroundColor: CORES.branco,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: CORES.borda,
    padding: 24,
    alignItems: 'center',
    marginBottom: 16,
  },

  nomePerfil: {
    color: CORES.textoPrincipal,
    fontSize: 19,
    fontWeight: '800',
    textAlign: 'center',
    marginTop: 14,
  },

  tipoPerfil: {
    color: CORES.azulPrincipal,
    fontSize: 13,
    fontWeight: '700',
    marginTop: 6,
  },

  cartaoInformacoes: {
    backgroundColor: CORES.branco,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: CORES.borda,
    padding: 20,
  },

  tituloInformacoes: {
    color: CORES.textoPrincipal,
    fontSize: 17,
    fontWeight: '800',
    marginBottom: 20,
  },

  areaSair: {
    marginTop: 20,
  },

  rodapePainel: {
    textAlign: 'center',
    color: CORES.textoDiscreto,
    fontSize: 11,
    marginTop: 6,
  },
});



// Respeita as barras nativas do dispositivo.
export default function App() {
  return (
    <SafeAreaProvider>
      <SafeAreaView
        style={{ flex: 1, backgroundColor: CORES.fundo }}
        edges={['left', 'right']}
      >
        <Aplicativo />
      </SafeAreaView>
    </SafeAreaProvider>
  );
}