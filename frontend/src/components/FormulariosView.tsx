import React, { useState } from 'react';
import {
  ClipboardList,
  CheckCircle2,
  Clock,
  Award,
  Sparkles,
  PlusCircle,
  BarChart2,
  Send,
  HelpCircle,
} from 'lucide-react';
import { Usuario, Formulario } from '../types/serena.types';
import { serenaApi } from '../services/serena-api.service';

interface FormulariosViewProps {
  currentUser: Usuario;
  formularios: Formulario[];
  onRefreshFormularios: () => void;
  onOpenCreateFormulario: () => void;
}

export const FormulariosView: React.FC<FormulariosViewProps> = ({
  currentUser,
  formularios = [],
  onRefreshFormularios,
  onOpenCreateFormulario,
}) => {
  const isPsicologo = currentUser.id_rol === 2;
  const [formularioActivo, setFormularioActivo] = useState<Formulario | null>(null);
  const [respuesta, setRespuesta] = useState('');
  const [completado, setCompletado] = useState(false);
  const [error, setError] = useState('');

  const pendientesCount = (formularios || []).filter((f) => !f.respondido).length;
  const completadosCount = (formularios || []).filter((f) => f.respondido).length;
  const porcentaje = Math.round((completadosCount / Math.max(1, (formularios || []).length)) * 100);

  const handleFinalizarCuestionario = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formularioActivo || !respuesta.trim()) return;
    setError('');
    try {
      await serenaApi.responderFormularioEnApi(formularioActivo.id_formulario, respuesta.trim());
      setCompletado(true);
      await onRefreshFormularios();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'No se pudo guardar la respuesta.');
    }
  };

  return (
    <div className="max-w-4xl mx-auto flex flex-col gap-4 pb-12">
      {/* Header */}
      <div className="bg-transparent rounded-2xl p-4 sm:p-5 border border-transparent hover:bg-white hover:border-slate-200/70 hover:shadow-xs transition-all duration-150 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl sm:text-2xl font-bold text-slate-900 tracking-tight">
            {isPsicologo ? 'Gestión y creación de formularios' : 'Encuestas de bienestar'}
          </h2>
          <p className="text-xs sm:text-sm text-slate-600 mt-1 max-w-2xl leading-relaxed">
            {isPsicologo
              ? 'Diseña nuevos instrumentos para evaluar factores de riesgo psicosocial y clima formativo en el CMTC.'
              : 'Diligencia tus encuestas periódicas. Tu retroalimentación permite diseñar mejores talleres y apoyos de salud mental.'}
          </p>
        </div>

        {isPsicologo && (
          <button
            onClick={onOpenCreateFormulario}
            className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-medium transition-colors flex items-center gap-1.5 cursor-pointer"
          >
            <PlusCircle className="w-4 h-4" />
            <span>Crear Nuevo Formulario</span>
          </button>
        )}
      </div>

      {/* VISTA APRENDIZ: BANNER FORMATIVO SOBRIO Y PLANO */}
      {!isPsicologo && (
        <div className="bg-transparent rounded-2xl p-4 sm:p-5 border border-transparent hover:bg-white hover:border-slate-200/70 hover:shadow-xs transition-all duration-150 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3.5">
            <div className="w-10 h-10 rounded-xl bg-[#7E22CE]/10 flex items-center justify-center shrink-0">
              <Award className="w-5 h-5 text-[#7E22CE]" />
            </div>
            <div>
              <h3 className="text-xs sm:text-sm font-bold text-slate-900">
                Tu voz construye el Bienestar SENA
              </h3>
              <p className="text-xs text-slate-500 mt-0.5 leading-relaxed max-w-md">
                Completar cada tamizaje te toma menos de 4 minutos y desbloquea recomendaciones personalizadas para tus jornadas de estudio.
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3 shrink-0">
            <div className="flex flex-col items-end">
              <span className="text-sm font-bold text-slate-900">{porcentaje}%</span>
              <span className="text-[10px] text-slate-400">Progreso Total</span>
            </div>
            <div className="w-24 bg-slate-200 h-2 rounded-full overflow-hidden">
              <div
                className="bg-[#7E22CE] h-full rounded-full transition-all duration-500"
                style={{ width: `${porcentaje}%` }}
              ></div>
            </div>
          </div>
        </div>
      )}

      {/* LISTA DE FORMULARIOS */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {formularios.map((form) => {
          return (
            <div
              key={form.id_formulario}
              className="bg-transparent rounded-2xl p-4 sm:p-5 border border-transparent hover:bg-white hover:border-slate-200/70 hover:shadow-xs transition-all duration-150 flex flex-col justify-between gap-3"
            >
              <div>
                <div className="flex items-center justify-between gap-2 mb-2">
                  <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
                    {form.id_comunidad || 'Sin comunidad'}
                  </span>
                  {form.respondido ? (
                    <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-[#EBF7E6] text-[#2E8500] flex items-center gap-1">
                      <CheckCircle2 className="w-3 h-3" />
                      Completado
                    </span>
                  ) : (
                    <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-amber-50 text-amber-700">
                      Pendiente por llenar
                    </span>
                  )}
                </div>

                <h4 className="text-xs sm:text-sm font-bold text-slate-900 mb-1 leading-snug">
                  {form.nombre_formulario}
                </h4>
                <p className="text-xs text-slate-500 leading-relaxed mb-2.5">
                  {form.descripcion}
                </p>

                <div className="flex items-center gap-3 text-[11px] text-slate-400 font-normal">
                  <span className="flex items-center gap-1">
                    <ClipboardList className="w-3.5 h-3.5 text-slate-400" />
                    {form.preguntas_count} preguntas
                  </span>
                  <span>•</span>
                  <span className="flex items-center gap-1">
                    <Clock className="w-3.5 h-3.5 text-slate-400" />
                    {form.tiempo_estimado || 'Sin estimación'}
                  </span>
                </div>
              </div>

              {/* Acciones */}
              <div className="pt-2.5 border-t border-slate-100 flex items-center justify-between">
                <span className="text-[10px] text-slate-400 font-normal">
                  {form.id_usuario === currentUser.id_usuario ? 'Creado por ti' : 'Formulario disponible'}
                </span>

                {!isPsicologo ? (
                  <button
                    onClick={() => {
                      setFormularioActivo(form);
                      setRespuesta('');
                      setError('');
                      setCompletado(false);
                    }}
                    disabled={form.respondido}
                    className={`px-3.5 py-1.5 rounded-xl text-xs font-medium transition-colors cursor-pointer ${
                      form.respondido
                        ? 'bg-slate-100 hover:bg-slate-200 text-slate-700'
                        : 'bg-slate-900 hover:bg-slate-800 text-white'
                    }`}
                  >
                    {form.respondido ? 'Respondido' : 'Responder →'}
                  </button>
                ) : (
                  <span className="text-xs text-slate-500 font-normal">
                    {form.respuestas_count ?? 0} respuestas
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Modal Interactivo de Diligenciamiento */}
      {formularioActivo && (
        <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-xl w-full p-6 sm:p-8 shadow-2xl border border-slate-200 animate-in fade-in zoom-in-95 duration-150 max-h-[90vh] flex flex-col">
            <div className="flex items-center justify-between pb-3 border-b border-slate-100 mb-4">
              <div>
                <h3 className="text-base font-extrabold text-slate-900">
                  {formularioActivo.nombre_formulario}
                </h3>
                <p className="text-xs text-slate-500">
                  {formularioActivo.descripcion}
                </p>
              </div>
              <button
                onClick={() => setFormularioActivo(null)}
                className="text-slate-400 hover:text-slate-600 font-bold"
              >
                ✕
              </button>
            </div>

            {completado ? (
              <div className="p-10 text-center flex flex-col items-center gap-3">
                <div className="w-16 h-16 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center">
                  <CheckCircle2 className="w-10 h-10" />
                </div>
                <h4 className="text-lg font-black text-slate-900">¡Muchas Gracias!</h4>
                <p className="text-xs text-slate-600 max-w-xs">
                  Tu formulario ha sido registrado exitosamente en SERENA.
                </p>
              </div>
            ) : (
              <form onSubmit={handleFinalizarCuestionario} className="flex-1 overflow-y-auto flex flex-col gap-4 pr-1">
                <label className="flex flex-col gap-2 text-xs font-bold text-slate-800">
                  Tu respuesta
                  <textarea
                    required
                    maxLength={5000}
                    rows={6}
                    value={respuesta}
                    onChange={(event) => setRespuesta(event.target.value)}
                    className="w-full rounded-xl border border-slate-200 p-3 font-normal"
                  />
                </label>

                {error && <p role="alert" className="text-xs text-rose-700">{error}</p>}

                <div className="pt-4 border-t border-slate-100 flex items-center justify-between">
                  <button
                    type="button"
                    onClick={() => setFormularioActivo(null)}
                    className="px-4 py-2 text-xs text-slate-600 hover:bg-slate-100 rounded-xl font-semibold"
                  >
                    Cancelar
                  </button>
                  <button
                    type="submit"
                    disabled={!respuesta.trim()}
                    className="px-6 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold shadow-md shadow-emerald-600/20"
                  >
                    Enviar Respuestas
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
